### Overview

The **SolTechnology.Core.Story** library provides workflow orchestration for multi-step
business processes. It supports both automated workflows and interactive workflows with
SQLite persistence. Built on the Tale Code philosophy — workflows read like prose.

### Installation

```bash
dotnet add package SolTechnology.Core.Story
```

### Registration

```csharp
// Default: in-memory persistence — supports both automated and interactive stories.
// Ideal for dev, tests, and single-process apps. Registers StoryManager +
// InMemoryStoryRepository.
services.RegisterStories();

// Production: durable SQLite persistence.
services.RegisterStories(StoryOptions.WithSqlitePersistence("stories.db"));

// Explicit opt-out: no repository, no StoryManager. Only fully automated
// TellStory() flows are allowed — running an InteractiveChapter fails with a
// clear, actionable error.
services.RegisterStories(StoryOptions.WithoutPersistence());

// Scan additional assemblies for chapters & handlers (MediatR-style).
services.RegisterStories(StoryOptions.WithInMemoryPersistence(),
    typeof(MySaveCityStory).Assembly,
    typeof(MyOtherStory).Assembly);

// Tweaks (mutable settable properties on the returned options).
var opts = StoryOptions.WithSqlitePersistence("stories.db");
opts.StopOnFirstError = false;
opts.StoryIdPrefix = "ORDER";
services.RegisterStories(opts);
```

> **Breaking change:** prior to this revision, `RegisterStories()` without arguments
> registered no persistence. It now defaults to in-memory. Use
> `StoryOptions.WithoutPersistence()` to recover the old behavior.

`RegisterStories` registers:

- All concrete `IChapter<>` implementations as **transient**.
- All concrete `StoryHandler<,,>` implementations as **transient**.
- `StoryHandlerRegistry` (singleton) — name-to-type whitelist used by `StoryController`.
- `StoryManager` (scoped) — when persistence is enabled.
- `IStoryRepository` (singleton) — the repository produced by the factory.

### Usage

#### 1. Automated story

```csharp
public class OrderInput  { public int OrderId { get; set; } }
public class OrderOutput { public string Status { get; set; } = ""; }

public class OrderContext : Context<OrderInput, OrderOutput>
{
    public string CustomerEmail { get; set; } = "";
    public decimal TotalAmount { get; set; }
}

public class ProcessOrderStory
    : StoryHandler<OrderInput, OrderContext, OrderOutput>
{
    public ProcessOrderStory(IServiceProvider sp, ILogger<ProcessOrderStory> logger)
        : base(sp, logger) { }

    protected override async Task TellStory()
    {
        await ReadChapter<ValidateOrderChapter>();
        await ReadChapter<ProcessPaymentChapter>();
        await ReadChapter<SendConfirmationChapter>();

        Context.Output.Status = "Completed";
    }
}

public class ValidateOrderChapter : Chapter<OrderContext>
{
    public override Task<Result> Read(OrderContext context)
        => context.Input.OrderId <= 0
            ? Result.FailAsTask("Invalid order ID")
            : Result.SuccessAsTask();
}
```

#### 2. Interactive story (pause / resume)

```csharp
public class PaymentInfo
{
    public string CardNumber { get; set; } = "";
    public string Cvv { get; set; } = "";
}

public class CollectPaymentInfoChapter
    : InteractiveChapter<OrderContext, PaymentInfo>
{
    public override Task<Result> ReadWithInput(OrderContext context, PaymentInfo userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput.CardNumber))
            return Result.FailAsTask("Card number is required");
        if (userInput.CardNumber.Length != 16)
            return Result.FailAsTask("Invalid card number");

        context.TotalAmount = 99m;
        return Result.SuccessAsTask();
    }
}
```

#### 3. Using `StoryManager` for pause / resume

```csharp
var input = new OrderInput { OrderId = 123 };

var start = await storyManager
    .StartStory<ProcessOrderStory, OrderInput, OrderContext, OrderOutput>(
        input,
        idempotencyKey: Request.Headers["Idempotency-Key"]);

if (start.IsSuccess && start.Data!.Status == StoryStatus.WaitingForInput)
{
    var storyId = start.Data.StoryId;
    var schema  = start.Data.CurrentChapter!.RequiredData;

    // …collect user input from UI…

    var userInput = JsonSerializer.SerializeToElement(
        new PaymentInfo { CardNumber = "1234567812345678", Cvv = "123" });

    var resume = await storyManager
        .ResumeStory<ProcessOrderStory, OrderInput, OrderContext, OrderOutput>(
            storyId,
            userInput);

    if (resume.IsSuccess && resume.Data!.Status == StoryStatus.Completed)
    {
        // done
    }
}
```

#### 4. Cancellation

```csharp
await storyManager.CancelStory(storyId);
```

#### 5. Direct handler usage (simple, no persistence)

```csharp
public class OrderController : ControllerBase
{
    private readonly ProcessOrderStory _story;
    public OrderController(ProcessOrderStory story) { _story = story; }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] OrderInput input)
    {
        var result = await _story.Handle(input, CancellationToken.None);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }
}
```

> **Note.** The handler is resolved via DI — `RegisterStories()` already registers every
> concrete `StoryHandler<,,>` found in the scanned assemblies.

### Pause-as-state (NOT pause-as-failure)

When a story pauses, `Handle(...)` returns `Result<TOutput>.Fail(new StoryPausedError(...))`.
`StoryManager` transparently converts that into `Result<StoryInstance>.Success(...)` with
`Status = WaitingForInput`. Detect pause with **type test**, never with string match:

```csharp
if (result.IsFailure && result.Error is StoryPausedError paused)
{
    // Story paused at paused.ChapterId inside paused.StoryId
}
```

Analogous: `StoryCancelledError`.

### Versioning

Handler versioning (compatibility checks on resume after redeploy) is **not currently
implemented** — see ADR-002 for the planned design (SemVer-based compatibility) under
"Future extensions". Today the engine accepts any persisted state regardless of how the
handler has changed; the developer is responsible for ensuring backward-compatible
chapter sequences and context shapes when redeploying with active in-flight stories.


### Error handling

Chapters return `Result.Success()` / `Result.Fail(...)`. By default the engine aborts on
first error (`StoryOptions.StopOnFirstError = true`). Set to `false` to collect all errors
into an `AggregateError`.

### REST API

Derive from `StoryController`:

```csharp
public class OrderStoryController : StoryController
{
    public OrderStoryController(
        StoryManager manager,
        StoryHandlerRegistry registry,
        StoryOptions options,
        ILogger<StoryController> logger)
        : base(manager, registry, options, logger) { }
}
```

Endpoints:

| Method | Path                          | Purpose                                     |
|--------|-------------------------------|---------------------------------------------|
| POST   | `/api/story/{handlerName}/start` | Start a new story (whitelisted handlers only) |
| POST   | `/api/story/{storyId}`           | Resume a paused story                      |
| DELETE | `/api/story/{storyId}`           | Cancel a running / paused story            |
| GET    | `/api/story/{storyId}`           | Current state                              |
| GET    | `/api/story/{storyId}/result`    | Deserialized output (Completed only)       |

`Idempotency-Key` header is honored on `/start` — retried calls with the same key return
the existing instance instead of creating a new one.

HTTP semantics:

- `Status.WaitingForInput` → `202 Accepted`
- `Status.Completed` → `200 OK`
- `Status.Failed` / not found → `4xx`

### Security notes

- Only handlers reachable through `StoryHandlerRegistry` are exposed — add authorization
  attributes (`[Authorize(...)]`) to your derived controller before exposing it publicly.
- Do not place secrets or PII in `Context`. For SQLite, prefer filesystem-level encryption
  or store references to an external secret store and load them on demand.
- `SqliteStoryRepository` validates the supplied path. Do not interpolate user-controlled
  strings into it.

### Observability

Every log entry emitted by the engine is scoped with `StoryId` and `StoryHandler`, so
configure your logger filters accordingly:

```csharp
logging.AddFilter("SolTechnology.Core.Story.Orchestration.StoryEngine", LogLevel.Information);
```

### Key features

- Narrative pipeline (`TellStory`, `ReadChapter<T>`).
- Typed context — no `dynamic`, no runtime reflection into your data.
- Interactive chapters with schema introspection for API consumers.
- Pause / resume with SQLite (WAL mode, retry on busy) or in-memory persistence.
- Idempotency-key deduplication, cancellation, listing.
- Strongly-typed error markers (`StoryPausedError`, `StoryCancelledError`).

### Related documentation

- [Story framework architecture (ADR-002)](./adr/002-Story-Framework-Implementation.md)
- [Tale Code philosophy](./Tale.md)
- [Review document](./reviews/Story-Framework-Review.md)


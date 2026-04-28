# SolTechnology.Core.Story

> **Workflows that read like prose.** A narrative-driven orchestration framework for
> multi-step business processes — automated pipelines, interactive sagas, durable long-running
> workflows. Pluggable persistence, typed lifecycle, zero magic.

[![NuGet](https://img.shields.io/nuget/v/SolTechnology.Core.Story.svg)](https://www.nuget.org/packages/SolTechnology.Core.Story/)

## Why Story Framework?

Most workflow engines force you to learn a DSL, fight a state machine, or accept a runtime
that hijacks your code. Story does the opposite — your workflow **is** a method that calls
chapters in order. Read the method, you understand the workflow.

- **📖 Tale Code philosophy** — `TellStory()` narrates what happens. Chapters are named as
  actions. The flow is linear and obvious.
- **🧩 First-class DI** — chapters and handlers are registered transients; injecting
  repositories, HTTP clients, mediators, anything Scoped — just works.
- **⏸ Pause & resume** — interactive chapters declare a typed input schema, the engine
  persists state, your API resumes the story when the user replies.
- **🔌 Pluggable persistence** — in-memory by default, SQLite for production, or bring your
  own (`IStoryRepository`) for Postgres / Cosmos / EF Core / whatever.
- **🛡 Typed lifecycle errors** — `StoryPausedError`, `StoryCancelledError` — never parse
  strings to detect state.
- **🆔 Idempotency built-in** — `Idempotency-Key` header / `idempotencyKey` parameter
  deduplicates retries automatically.
- **🌐 Opt-in REST API** — inherit `StoryController`, get `start` / `resume` / `cancel` /
  `state` endpoints with the right HTTP semantics out of the box.
- **🚫 No `dynamic`, no reflection into your data** — fully generic, fully typed, fully
  refactor-safe.

## Install

```bash
dotnet add package SolTechnology.Core.Story
```

## Quick start

### 1. Define input, context and output

```csharp
public record SaveCityInput(string CityName);
public class SaveCityOutput { public string CityId { get; set; } = ""; }

public class SaveCityContext : Context<SaveCityInput, SaveCityOutput>
{
    public bool Exists { get; set; }
}
```

### 2. Write chapters

```csharp
public class LoadExistingCity : Chapter<SaveCityContext>
{
    public override Task<Result> Read(SaveCityContext ctx)
    {
        ctx.Exists = ctx.Input.CityName == "Paris";
        return Result.SuccessAsTask();
    }
}

public class SaveCity : Chapter<SaveCityContext>
{
    public override Task<Result> Read(SaveCityContext ctx)
    {
        ctx.Output.CityId = ctx.Exists ? "1" : Guid.NewGuid().ToString();
        return Result.SuccessAsTask();
    }
}
```

### 3. Tell the story

```csharp
public class SaveCityStory
    : StoryHandler<SaveCityInput, SaveCityContext, SaveCityOutput>
{
    public SaveCityStory(IServiceProvider sp, ILogger<SaveCityStory> log)
        : base(sp, log) { }

    protected override async Task TellStory()
    {
        await ReadChapter<LoadExistingCity>();
        await ReadChapter<SaveCity>();
    }
}
```

### 4. Register and run

```csharp
// One line — defaults to in-memory persistence so interactive stories work immediately.
services.RegisterStories();

var story = sp.GetRequiredService<SaveCityStory>();
var result = await story.Handle(new SaveCityInput("Paris"), CancellationToken.None);
// result.Data.CityId == "1"
```

That's it. No DSL. No state machine. No `[Activity]` attributes. Just a method that reads
top-to-bottom.

## Core concepts

### `StoryHandler<TInput, TContext, TOutput>`

The orchestrator. Define your workflow as a sequence of `ReadChapter<T>()` calls in
`TellStory()`. Compatible with CQRS — the same handler is also a `IQueryHandler` /
`ICommandHandler`. Auto-registered as `Transient` by `RegisterStories()`.

### `Context<TInput, TOutput>`

The state object that flows between chapters. Holds `Input`, `Output`, and any
intermediate values you want to share between chapters.

### `Chapter<TContext>`

A unit of business logic. Returns `Result.Success()` or `Result.Fail("reason")`. Resolved
from DI, so it can declare any dependencies in its constructor.

```csharp
public class LoadExistingCity : Chapter<SaveCityContext>
{
    private readonly ICityRepository _repository;

    public LoadExistingCity(ICityRepository repository) => _repository = repository;

    public override async Task<Result> Read(SaveCityContext ctx)
    {
        ctx.ExistingCity = await _repository.FindByName(ctx.Input.CityName);
        return Result.Success();
    }
}
```

**Best practices**

- Keep each chapter focused on one thing. If the name needs an "And", split it.
- Inject what you need — chapters are `Transient`, constructor injection is free.
- Return `Result.Fail("reason")` instead of throwing. Exceptions are caught and wrapped,
  but explicit failures produce cleaner error trails.
- Don't mutate `ctx.Input` — treat it as read-only. Write intermediate data as new
  properties on the `Context`.
- Don't populate `ctx.Output` until the final chapter — keeps partial failures from
  leaking half-baked results.

### `InteractiveChapter<TContext, TChapterInput>`

A chapter that pauses the story and waits for caller input. Declares its expected input
shape so consumers (e.g. a SPA, a form generator, OpenAPI-driven clients) can render the
right UI without hardcoding field lists:

```csharp
public class RequestCustomerDetails
    : InteractiveChapter<OrderContext, CustomerDetails>
{
    public override List<DataField> GetRequiredInputSchema() => new()
    {
        new() { Name = "Name",    Type = "string", Required = true  },
        new() { Name = "Email",   Type = "string", Required = true  },
        new() { Name = "Address", Type = "string", Required = false },
    };

    public override Task<Result> ReadWithInput(OrderContext ctx, CustomerDetails input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            return Result.FailAsTask("Customer name is required");
        }

        ctx.CustomerName  = input.Name;
        ctx.CustomerEmail = input.Email;
        return Result.SuccessAsTask();
    }
}
```

Minimal variant — just the logic, schema inferred from `TChapterInput` via reflection:

```csharp
public class CollectEmailChapter : InteractiveChapter<OrderContext, EmailInput>
{
    public override Task<Result> ReadWithInput(OrderContext ctx, EmailInput input)
    {
        if (!input.Email.Contains("@"))
        {
            return Result.FailAsTask("Invalid e-mail");
        }

        ctx.CustomerEmail = input.Email;
        return Result.SuccessAsTask();
    }
}
```

**Best practices**

- Validate inside `ReadWithInput` — treat the paused input as untrusted. Return
  `Result.Fail` with a human-readable reason; the error surfaces in the HTTP response.
- Keep `TChapterInput` narrow. One chapter, one conceptual step of user interaction.
- Override `GetRequiredInputSchema()` only when you need hand-tuned metadata (hints,
  default values, richer types). Otherwise, let reflection derive it.

#### Use cases

##### Order checkout with pause-for-customer-details

A classic e-commerce flow. Validation and inventory check run automatically; the story
pauses to collect customer details from the user; payment and confirmation run after the
resume.

```csharp
public class OrderProcessingStory
    : StoryHandler<OrderInput, OrderContext, OrderOutput>
{
    public OrderProcessingStory(IServiceProvider sp, ILogger<OrderProcessingStory> log)
        : base(sp, log) { }

    protected override async Task TellStory()
    {
        await ReadChapter<ValidateOrder>();         // automated
        await ReadChapter<ReserveInventory>();      // automated
        await ReadChapter<RequestCustomerDetails>();// ⏸ pause — interactive
        await ReadChapter<ProcessPayment>();        // automated, runs on resume
        await ReadChapter<SendConfirmation>();      // automated
    }
}
```

Driving the lifecycle from your application code:

```csharp
var manager = sp.GetRequiredService<StoryManager>();

// 1. Start — runs up to the first interactive chapter.
var start = await manager.StartStory<OrderProcessingStory, OrderInput, OrderContext, OrderOutput>(
    new OrderInput { Cart = cart });

if (start.IsSuccess && start.Data!.Status == StoryStatus.WaitingForInput)
{
    var storyId = start.Data.StoryId;

    // 2. Inspect the schema required for the paused chapter — render a form from it.
    foreach (var field in start.Data.CurrentChapter!.RequiredData)
    {
        Console.WriteLine($"  {field.Name} ({field.Type}) {(field.Required ? "*" : "")}");
    }

    // 3. Later, after the user submits, resume with the typed payload.
    var userInput = JsonSerializer.SerializeToElement(new CustomerDetails
    {
        Name  = "John Doe",
        Email = "john@example.com",
    });

    var resume = await manager.ResumeStory<OrderProcessingStory, OrderInput, OrderContext, OrderOutput>(
        storyId, userInput);

    if (resume.IsSuccess && resume.Data!.Status == StoryStatus.Completed)
    {
        // Payment processed, confirmation sent.
    }
}
```

Between start and resume, pull a snapshot of the story state any time — audit logs,
dashboards, "resume later" links in an email:

```csharp
var state = await manager.GetStoryState(storyId);
Console.WriteLine($"Status: {state.Data!.Status}");
Console.WriteLine($"Current: {state.Data.CurrentChapter?.ChapterId}");
Console.WriteLine($"History: {state.Data.History.Count} chapters executed");
```

##### Approval workflow with multiple pause points

A request-for-approval pipeline where each approver pauses the story in turn. Same
mechanism, different shape — two interactive chapters in sequence.

```csharp
public class ExpenseApprovalStory
    : StoryHandler<ExpenseInput, ExpenseContext, ExpenseOutput>
{
    public ExpenseApprovalStory(IServiceProvider sp, ILogger<ExpenseApprovalStory> log)
        : base(sp, log) { }

    protected override async Task TellStory()
    {
        await ReadChapter<ClassifyExpense>();           // automated
        await ReadChapter<ManagerApprovalChapter>();    // ⏸ pause — manager signs off
        await ReadChapter<FinanceApprovalChapter>();    // ⏸ pause — finance signs off (only if > threshold)
        await ReadChapter<PostToLedger>();              // automated
        await ReadChapter<NotifyRequester>();           // automated
    }
}

public class ManagerApprovalChapter
    : InteractiveChapter<ExpenseContext, ApprovalDecision>
{
    public override Task<Result> ReadWithInput(ExpenseContext ctx, ApprovalDecision decision)
    {
        if (!decision.Approved)
        {
            return Result.FailAsTask($"Rejected by manager: {decision.Reason}");
        }

        ctx.ManagerApproval = decision;
        return Result.SuccessAsTask();
    }
}
```

Two real callers, two resumes — between them the story sits persisted in the repository.
Persistence survives process restarts, so the manager can approve on Monday and finance
on Wednesday.

##### User onboarding with progressive disclosure

Long-form interactive flow — collect minimum info up front, pause, collect more, pause,
etc. The `Context` accumulates data between pauses; each interactive chapter only cares
about *its* slice.

```csharp
public class UserOnboardingStory
    : StoryHandler<OnboardingInput, OnboardingContext, OnboardingOutput>
{
    public UserOnboardingStory(IServiceProvider sp, ILogger<UserOnboardingStory> log)
        : base(sp, log) { }

    protected override async Task TellStory()
    {
        await ReadChapter<CollectBasicInfoChapter>();      // ⏸ name, email
        await ReadChapter<SendVerificationEmail>();        // automated
        await ReadChapter<VerifyEmailChapter>();           // ⏸ verification code
        await ReadChapter<CollectPreferencesChapter>();    // ⏸ preferences
        await ReadChapter<CompleteOnboardingChapter>();    // automated — creates account
    }
}
```

The engine automatically skips chapters already recorded in `History` when resuming, so
refreshing the browser or retrying the request is safe.

### `StoryManager`

Orchestrates persisted workflows: `StartStory`, `ResumeStory`, `CancelStory`,
`GetStoryState`. Creates a fresh DI scope per invocation, so Scoped dependencies
(`DbContext`, EF Core, per-request services) work correctly across pause/resume
boundaries.

### `StoryController` *(opt-in REST API)*

Inherit and add your auth attributes. You get the four canonical endpoints, with proper
HTTP semantics:

| Method | Route | Returns |
|---|---|---|
| `POST` | `/api/story/{handlerName}/start` | `200 OK` (completed) or `202 Accepted` (paused) |
| `POST` | `/api/story/{storyId}` | resume with body as user input |
| `GET`  | `/api/story/{storyId}` | current state |
| `GET`  | `/api/story/{storyId}/result` | deserialized output (only if `Completed`) |
| `DELETE` | `/api/story/{storyId}` | cancel |

Only handlers registered through `RegisterStories()` are exposed (whitelist via
`StoryHandlerRegistry`). `Idempotency-Key` HTTP header is honored.

## Persistence

Persistence providers plug in through the builder returned by `RegisterStories()`. The
default is in-memory, so interactive stories work the moment you call `RegisterStories()`
with no arguments.

```csharp
// Default: in-memory (dev/test/single-process).
services.RegisterStories();
// equivalent to:
services.RegisterStories().UseInMemoryStoryRepository();

// Production: SQLite (WAL journal, retries on SQLITE_BUSY, indexed).
services.RegisterStories().UseSqliteStoryRepository("Data Source=stories.db");

// Production with full tuning:
services.RegisterStories().UseSqliteStoryRepository(o =>
{
    o.ConnectionString = "Data Source=stories.db;Cache=Shared";
    o.MaxRetries       = 5;
    o.EnableWalMode    = true;
});

// Bring your own backend — Postgres, Cosmos, EF Core, anything that implements IStoryRepository:
services.RegisterStories()
        .UseStoryRepository<MyPostgresStoryRepository>(ServiceLifetime.Scoped);
```

> Implementing a custom backend? `IStoryRepository` is a five-method interface
> (`FindById`, `FindByIdempotencyKey`, `ListAsync`, `SaveAsync`, `DeleteAsync`).
> See `InMemoryStoryRepository` and `SqliteStoryRepository` for reference implementations.

## Pause & resume

Detecting pause — type-check the error, never string-match:

```csharp
var start = await manager.StartStory<OrderStory, OrderInput, OrderContext, OrderOutput>(input);

if (start.IsSuccess && start.Data!.Status == StoryStatus.WaitingForInput)
{
    // Render a form using start.Data.CurrentChapter.RequiredData …
}

// later, when the user replies:
var resume = await manager.ResumeStory<OrderStory, OrderInput, OrderContext, OrderOutput>(
    start.Data.StoryId,
    JsonSerializer.SerializeToElement(new EmailInput { Email = "x@y.z" }));
```

Pause is also surfaced as a typed `Result.Fail`:

```csharp
if (result.Error is StoryPausedError paused)
{
    // paused.StoryId, paused.ChapterId
}
```

## Idempotency

```csharp
await manager.StartStory<…>(input, idempotencyKey: "order-42");
```

Retries with the same key return the existing story instead of starting a new one. Works
through the HTTP `Idempotency-Key` header on `StoryController.StartStory` too.

## Cancellation

```csharp
await manager.CancelStory(storyId);
```

Status becomes `Cancelled`. Subsequent resume attempts fail cleanly.

## Error handling

Chapters return `Result.Success()` / `Result.Fail("reason")`. The engine aggregates errors
into an `AggregateError` when `StoryOptions.StopOnFirstError = false`.

Marker error types — detect by **type**, never by string:

| Error | Meaning |
|---|---|
| `StoryPausedError` | Story is waiting for user input at an interactive chapter. |
| `StoryCancelledError` | Story was cancelled by token or `CancelStory`. |

**Best practices**

- Use `is StoryPausedError` / `is StoryCancelledError` in callers. Never `Message.Contains(...)`.
- Prefer `Result.Fail("business reason")` over `throw` in chapter bodies — exceptions are
  wrapped but your reason string is clearer than a stack trace.
- Set `StopOnFirstError = false` only when chapters are independent and you actually want
  the full error list (e.g. batch validation). For sequential flows keep the default.

## Versioning

Handler versioning is **not currently implemented**. See ADR-002
("Future extensions → Handler versioning") for the planned SemVer-based compatibility
design. Today the engine accepts any persisted state regardless of how the handler has
changed; you are responsible for keeping chapter sequences and context shapes
backward-compatible when redeploying with in-flight stories.

## Registration API

```csharp
public static IStoryBuilder RegisterStories(
    this IServiceCollection services,
    Action<StoryOptions>? configure = null,
    params Assembly[] assemblies);
```

Returns `IStoryBuilder` for chaining a persistence provider:

- `.UseInMemoryStoryRepository()` — default; explicit for clarity.
- `.UseSqliteStoryRepository("Data Source=…")` — connection string shortcut.
- `.UseSqliteStoryRepository(opts => …)` — full options callback.
- `.UseStoryRepository<TRepository>(ServiceLifetime)` — your own backend.

A persistence provider is always present — the minimum is in-memory.

If no assemblies are passed, the entry assembly and the calling assembly are scanned for
`IChapter<>` and `StoryHandler<,,>` implementations.

`StoryOptions` (configurable via the callback) — engine-level policies:

| Option | Default | Effect |
|---|---|---|
| `StopOnFirstError` | `true` | Halt the story on the first chapter failure (vs. aggregate). |
| `StoryIdPrefix` | `"STR"` | Prefix for generated `Auid` story identifiers. |
| `RestrictControllerToRegisteredHandlers` | `true` | Whitelist enforcement on `StoryController`. |

## Not supported (yet)

Parallel chapter execution, durable retries with backoff, cross-process sagas / compensation,
distributed tracing via `ActivitySource`, handler versioning. Tracked in
[`docs/reviews/Story-Framework-Review.md`](../../docs/reviews/Story-Framework-Review.md)
and [ADR-002](../../docs/adr/002-Story-Framework-Implementation.md).

## Learn more

- [Full user guide (`docs/Story.md`)](../../docs/Story.md)
- [Tale Code philosophy (`docs/Tale.md`)](../../docs/Tale.md)
- [Architecture decision record (ADR-002)](../../docs/adr/002-Story-Framework-Implementation.md)

## License

Part of the [SolTechnology.Core](https://github.com/SolarFr/SolTechnology.Core) framework.

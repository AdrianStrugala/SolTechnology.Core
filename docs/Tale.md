# SolTechnology.Core.Tale

> **Workflows that read like prose.** A narrative-driven orchestration framework for
> multi-step business processes — automated pipelines, interactive sagas, durable long-running
> workflows. Pluggable persistence, typed lifecycle, zero magic.

[![NuGet](https://img.shields.io/nuget/v/SolTechnology.Core.Tale.svg)](https://www.nuget.org/packages/SolTechnology.Core.Tale/)

## Why Tale?

Most workflow engines force you to learn a DSL, fight a state machine, or accept a runtime
that hijacks your code. Tale does the opposite — your workflow **is** a `Tale`: a fluent table
of contents the engine reads top-to-bottom.

- **📖 Tale Code philosophy** — `Tell()` returns a `Tale` that narrates what happens. Chapters
  are named as actions, chained with `Open`/`Read`. The flow is linear and obvious.
- **🧩 First-class DI** — chapters and handlers are registered transients; inject repositories,
  HTTP clients, mediators, anything Scoped — it just works.
- **⏸ Pause & resume** — interactive chapters declare a typed input schema, the engine persists
  state, your API resumes the tale when the user replies.
- **🔌 Pluggable persistence** — in-memory by default, or bring your own
  (`ITaleRepository`) for SQLite / Postgres / Cosmos / EF Core / whatever.
  See `DreamTravel.SQLite` for a production-ready SQLite reference implementation.
- **🛡 Typed lifecycle errors** — `TalePausedError`, `TaleCancelledError` — never parse strings
  to detect state.
- **🆔 Idempotency built-in** — `Idempotency-Key` header / `idempotencyKey` parameter deduplicates
  retries automatically.
- **🌐 Opt-in REST API** — inherit `TaleController`, get `start` / `resume` / `cancel` / `state`
  endpoints with the right HTTP semantics out of the box.

## Installation

```bash
dotnet add package SolTechnology.Core.Tale
```

## Registration

```csharp
// In-memory persistence (default). Ideal for dev, tests, and single-process apps.
services.AddSolTale();

// Scan additional assemblies for chapters & handlers.
services.AddSolTale(
    configure: opts => opts.TaleIdPrefix = "ORDER",
    assemblies: typeof(MySaveCityTale).Assembly);

// Durable SQLite persistence — provided by the DreamTravel sample (DreamTravel.SQLite).
// Copy the sample project into your app and reference it, then:
services.AddSolTale(assemblies: typeof(MySaveCityTale).Assembly)
    .UseTaleRepository<SQLiteTaleRepository>();

// Bring your own backend — Postgres, Cosmos, EF Core, anything implementing ITaleRepository:
services.AddSolTale()
    .UseTaleRepository<MyPostgresTaleRepository>(ServiceLifetime.Scoped);
```


`AddSolTale` registers:

- All concrete `IChapter<>` implementations as **transient**.
- All concrete `TaleHandler<,,>` implementations as **transient**.
- `TaleHandlerRegistry` (singleton) — name-to-type whitelist used by `TaleController`.
- `TaleManager` (scoped) — the orchestrator.
- `ITaleRepository` (singleton) — in-memory by default; swapped via `UseTaleRepository<T>()`.

If no assemblies are passed, the entry assembly and the calling assembly are scanned for
`IChapter<>` and `TaleHandler<,,>` implementations.

`TaleOptions` — engine-level policies:

| Option | Default | Effect |
|---|---|---|
| `TaleIdPrefix` | `"STR"` | Prefix for generated `Auid` tale identifiers. |
| `RestrictControllerToRegisteredHandlers` | `true` | Whitelist enforcement on `TaleController`. |

## Quick start

### 1. Define input, context and output

```csharp
public class OrderInput  { public int OrderId { get; set; } }
public class OrderOutput { public string Status { get; set; } = ""; }

public class OrderContext : Context<OrderInput, OrderOutput>
{
    public string CustomerEmail { get; set; } = "";
    public decimal TotalAmount { get; set; }
}
```

### 2. Write chapters

```csharp
public class ValidateOrderChapter : Chapter<OrderContext>
{
    public override Task<Result> Read(OrderContext context)
        => context.Input.OrderId <= 0
            ? Result.FailAsTask("Invalid order ID")
            : Result.SuccessAsTask();
}
```

### 3. Tell the tale

```csharp
public class ProcessOrderTale
    : TaleHandler<OrderInput, OrderContext, OrderOutput>
{
    public ProcessOrderTale(IServiceProvider sp, ILogger<ProcessOrderTale> logger)
        : base(sp, logger) { }

    protected override Tale<OrderOutput> Tell() =>
        Open<ValidateOrderChapter>()
            .Read<ProcessPaymentChapter>()
            .Read<SendConfirmationChapter>()
            .Do(ctx => ctx.Output.Status = "Completed")
            .Finale(ctx => ctx.Output);
}
```

### 4. Register and run

```csharp
services.AddSolTale();

var tale = sp.GetRequiredService<ProcessOrderTale>();
var result = await tale.Handle(new OrderInput { OrderId = 42 }, CancellationToken.None);
```

That's it. No DSL. No state machine. No `[Activity]` attributes. Just a `Tale` that reads
top-to-bottom.

## Core concepts

### `TaleHandler<TInput, TContext, TOutput>`

The orchestrator. Describe your workflow as a `Tale` in `Tell()` — `Open<T>()` reads the first
chapter, `.Read<T>()` chains the next, `.Finale(ctx => ctx.Output)` concludes. Compatible with
CQRS — the same handler is also a `IQueryHandler` / `ICommandHandler`. Auto-registered as
`Transient` by `AddSolTale()`.

> `Tell()` must be **deterministic**. It is re-invoked on every `Handle` call — including each
> resume of a paused tale — and the engine replays the rebuilt plan against the persisted chapter
> history. Branch on context state via `Expect` / `Otherwise`, never on ambient inputs (clock,
> random, feature flags) that can differ between the original run and a resume.

### `Context<TInput, TOutput>`

The state object that flows between chapters. Holds `Input`, `Output`, and any intermediate
values you want to share. State flows through the `Context`, not through return values — chapters
return only a `Result` to signal success or failure.

### `Chapter<TContext>`

A unit of business logic. Returns `Result.Success()` or `Result.Fail("reason")`. Resolved from
DI, so it can declare any dependencies in its constructor.

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
- Don't mutate `ctx.Input` — treat it as read-only. Write intermediate data as new properties
  on the `Context`.
- Don't populate `ctx.Output` until the final chapter — keeps partial failures from leaking
  half-baked results.

### `InteractiveChapter<TContext, TChapterInput>`

A chapter that pauses the tale and waits for caller input. Declares its expected input shape so
consumers (a SPA, a form generator, OpenAPI-driven clients) can render the right UI without
hardcoding field lists:

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

- Validate inside `ReadWithInput` — treat the paused input as untrusted. Return `Result.Fail`
  with a human-readable reason; the error surfaces in the HTTP response.
- Keep `TChapterInput` narrow. One chapter, one conceptual step of user interaction.
- Override `GetRequiredInputSchema()` only when you need hand-tuned metadata (hints, default
  values, richer types). Otherwise, let reflection derive it.

### `TaleManager`

Orchestrates persisted workflows: `StartStory`, `ResumeStory`, `CancelStory`, `GetStoryState`.
Creates a fresh DI scope per invocation, so Scoped dependencies (`DbContext`, EF Core,
per-request services) work correctly across pause/resume boundaries.

## Use cases

### Order checkout with pause-for-customer-details

A classic e-commerce flow. Validation and inventory check run automatically; the tale pauses to
collect customer details from the user; payment and confirmation run after the resume.

```csharp
public class OrderProcessingTale
    : TaleHandler<OrderInput, OrderContext, OrderOutput>
{
    public OrderProcessingTale(IServiceProvider sp, ILogger<OrderProcessingTale> log)
        : base(sp, log) { }

    protected override Tale<OrderOutput> Tell() =>
        Open<ValidateOrder>()                  // automated
            .Read<ReserveInventory>()          // automated
            .Read<RequestCustomerDetails>()    // ⏸ pause — interactive
            .Read<ProcessPayment>()            // automated, runs on resume
            .Read<SendConfirmation>()          // automated
            .Finale(ctx => ctx.Output);
}
```

Driving the lifecycle from your application code:

```csharp
var manager = sp.GetRequiredService<TaleManager>();

// 1. Start — runs up to the first interactive chapter.
var start = await manager.StartStory<OrderProcessingTale, OrderInput, OrderContext, OrderOutput>(
    new OrderInput { Cart = cart });

if (start.IsSuccess && start.Data!.Status == TaleStatus.WaitingForInput)
{
    var taleId = start.Data.TaleId;

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

    var resume = await manager.ResumeStory<OrderProcessingTale, OrderInput, OrderContext, OrderOutput>(
        taleId, userInput);

    if (resume.IsSuccess && resume.Data!.Status == TaleStatus.Completed)
    {
        // Payment processed, confirmation sent.
    }
}
```

Between start and resume, pull a snapshot of the tale state any time — audit logs, dashboards,
"resume later" links in an email:

```csharp
var state = await manager.GetStoryState(taleId);
Console.WriteLine($"Status: {state.Data!.Status}");
Console.WriteLine($"Current: {state.Data.CurrentChapter?.ChapterId}");
Console.WriteLine($"History: {state.Data.History.Count} chapters executed");
```

### Approval workflow with multiple pause points

A request-for-approval pipeline where each approver pauses the tale in turn. Same mechanism,
different shape — two interactive chapters in sequence.

```csharp
public class ExpenseApprovalTale
    : TaleHandler<ExpenseInput, ExpenseContext, ExpenseOutput>
{
    public ExpenseApprovalTale(IServiceProvider sp, ILogger<ExpenseApprovalTale> log)
        : base(sp, log) { }

    protected override Tale<ExpenseOutput> Tell() =>
        Open<ClassifyExpense>()                    // automated
            .Read<ManagerApprovalChapter>()        // ⏸ pause — manager signs off
            .Read<FinanceApprovalChapter>()        // ⏸ pause — finance signs off (only if > threshold)
            .Read<PostToLedger>()                  // automated
            .Read<NotifyRequester>()               // automated
            .Finale(ctx => ctx.Output);
}
```

Two real callers, two resumes — between them the tale sits persisted in the repository.
Persistence survives process restarts, so the manager can approve on Monday and finance on
Wednesday.

### User onboarding with progressive disclosure

Long-form interactive flow — collect minimum info up front, pause, collect more, pause, etc. The
`Context` accumulates data between pauses; each interactive chapter only cares about *its* slice.

```csharp
public class UserOnboardingTale
    : TaleHandler<OnboardingInput, OnboardingContext, OnboardingOutput>
{
    public UserOnboardingTale(IServiceProvider sp, ILogger<UserOnboardingTale> log)
        : base(sp, log) { }

    protected override Tale<OnboardingOutput> Tell() =>
        Open<CollectBasicInfoChapter>()            // ⏸ name, email
            .Read<SendVerificationEmail>()         // automated
            .Read<VerifyEmailChapter>()            // ⏸ verification code
            .Read<CollectPreferencesChapter>()     // ⏸ preferences
            .Read<CompleteOnboardingChapter>()     // automated — creates account
            .Finale(ctx => ctx.Output);
}
```

The engine automatically skips chapters already recorded in `History` when resuming, so refreshing
the browser or retrying the request is safe.

### Direct handler usage (simple, no persistence)

For a fully automated tale you can skip `TaleManager` and call the handler directly — it is a
plain CQRS handler:

```csharp
public class OrderController : ControllerBase
{
    private readonly ProcessOrderTale _tale;
    public OrderController(ProcessOrderTale tale) { _tale = tale; }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] OrderInput input)
    {
        var result = await _tale.Handle(input, CancellationToken.None);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }
}
```

## Pause as state (NOT pause as failure)

When a tale pauses, `Handle(...)` returns `Result<TOutput>.Fail(new TalePausedError(...))`.
`TaleManager` transparently converts that into `Result<TaleInstance>.Success(...)` with
`Status = WaitingForInput`. Detect pause with a **type test**, never with a string match:

```csharp
if (result.IsFailure && result.Error is TalePausedError paused)
{
    // Tale paused at paused.ChapterId inside paused.TaleId
}
```

Analogous: `TaleCancelledError`.

## Persistence

Persistence providers plug in through the builder returned by `AddSolTale()`. The default is
in-memory, so interactive tales work the moment you call `AddSolTale()` with no arguments.

```csharp
// Default: in-memory (dev/test/single-process).
services.AddSolTale();
// equivalent to:
services.AddSolTale().UseInMemoryTaleRepository();

// Production: SQLite — provided by the DreamTravel sample (DreamTravel.SQLite).
// Copy it into your app, reference it, then:
services.AddSolTale()
    .UseTaleRepository<SQLiteTaleRepository>();

// Bring your own backend — Postgres, Cosmos, EF Core, anything that implements ITaleRepository:
services.AddSolTale()
        .UseTaleRepository<MyPostgresTaleRepository>(ServiceLifetime.Scoped);
```

> Implementing a custom backend? `ITaleRepository` is a five-method interface
> (`FindById`, `FindByIdempotencyKey`, `ListAsync`, `SaveAsync`, `DeleteAsync`).
> See `InMemoryTaleRepository` (in-box) and the sample's `SQLiteTaleRepository`
> (`DreamTravel.SQLite`) for reference implementations.

## Idempotency

```csharp
await manager.StartStory<…>(input, idempotencyKey: "order-42");
```

Retries with the same key return the existing tale instead of starting a new one. Works through
the HTTP `Idempotency-Key` header on `TaleController.StartStory` too.

## Cancellation

```csharp
await manager.CancelStory(taleId);
```

Status becomes `Cancelled`. Subsequent resume attempts fail cleanly.

## Error handling

Chapters return `Result.Success()` / `Result.Fail("reason")`. The tale runs on two tracks —
**won** or **lost**. The first failed chapter switches the tale to the lost track and the
remaining chapters are skipped; the failing `Error` becomes the tale result. Recover from an
acceptable failure with `.Otherwise<FallbackChapter>()` (or an inline `.Otherwise(ctx => …)`) to
switch back to the won track.

Marker error types — detect by **type**, never by string:

| Error | Meaning |
|---|---|
| `TalePausedError` | Tale is waiting for user input at an interactive chapter. |
| `TaleCancelledError` | Tale was cancelled by token or `CancelStory`. |

**Best practices**

- Use `is TalePausedError` / `is TaleCancelledError` in callers. Never `Message.Contains(...)`.
- Prefer `Result.Fail("business reason")` over `throw` in chapter bodies — exceptions are wrapped
  but your reason string is clearer than a stack trace.
- Recover from an acceptable failure with `.Otherwise<FallbackChapter>()` (or an inline
  `.Otherwise(ctx => …)`) instead of letting it abort the tale.

## REST API

Inherit `TaleController` and add your auth attributes:

```csharp
public class OrderTaleController : TaleController
{
    public OrderTaleController(
        TaleManager manager,
        TaleHandlerRegistry registry,
        TaleOptions options,
        ILogger<TaleController> logger)
        : base(manager, registry, options, logger) { }
}
```

Endpoints:

| Method | Path | Purpose |
|--------|------|---------|
| POST   | `/api/tale/{handlerName}/start` | Start a new tale (whitelisted handlers only) |
| POST   | `/api/tale/{taleId}`             | Resume a paused tale |
| DELETE | `/api/tale/{taleId}`             | Cancel a running / paused tale |
| GET    | `/api/tale/{taleId}`             | Current state |
| GET    | `/api/tale/{taleId}/result`      | Deserialized output (Completed only) |

`Idempotency-Key` header is honored on `/start` — retried calls with the same key return the
existing instance instead of creating a new one.

HTTP semantics:

- `Status.WaitingForInput` → `202 Accepted`
- `Status.Completed` → `200 OK`
- `Status.Failed` / not found → `4xx`

Only handlers registered through `AddSolTale()` are exposed (whitelist via
`TaleHandlerRegistry`).

## Security notes

- Only handlers reachable through `TaleHandlerRegistry` are exposed — add authorization
  attributes (`[Authorize(...)]`) to your derived controller before exposing it publicly.
- Do not place secrets or PII in `Context`. For SQLite persistence, prefer filesystem-level
  encryption or store references to an external secret store and load them on demand.
- The sample `SQLiteTaleRepository` validates the supplied path. Do not interpolate
  user-controlled strings into connection strings.

## Observability

Every log entry emitted by the engine is scoped with `TaleId` and `TaleHandler`, so configure
your logger filters accordingly:

```csharp
logging.AddFilter("SolTechnology.Core.Tale.Orchestration.TaleEngine", LogLevel.Information);
```

## Versioning

Handler versioning (compatibility checks on resume after redeploy) is **not currently
implemented** — see ADR-002 ("Future extensions → Handler versioning") for the planned
SemVer-based design. Today the engine accepts any persisted state regardless of how the handler
has changed; you are responsible for keeping chapter sequences and context shapes
backward-compatible when redeploying with in-flight tales.

## Not supported (yet)

Parallel chapter execution, durable retries with backoff, cross-process sagas / compensation,
distributed tracing via `ActivitySource`, handler versioning. Tracked in
[ADR-002](./adr/002-Story-Framework-Implementation.md).

## Working with AI Agent

Writing a Tale with an AI assistant (GitHub Copilot, Claude Code)? The repository ships a
**skill** — a narrow, file-cited procedure your agent can read on demand:

- [`command-query-event-tale`](https://github.com/AdrianStrugala/SolTechnology.Core/blob/master/.github/skills/command-query-event-tale/SKILL.md)
  — decide when a handler becomes a Tale, keep the `Tell()` Tale logic-free, name chapters
  one-verb-per-file, flow state through the `Context`, and choose where the Tale lives
  (`Commands`/`Queries` vs a domain-model `DomainServices` Tale vs a persisted interactive
  `Workflows` Tale).

It points at the binding rules in the Coding Guide — [§4 — Tale framework](https://github.com/AdrianStrugala/SolTechnology.Core/blob/master/docs/ClaudeCodingGuide.md)
keeps the anatomy and chapter rules in one place.

## Related documentation

- [Tale Code philosophy (`docs/ClaudeCodingGuide.md`)](./ClaudeCodingGuide.md)
- [Tale framework architecture (ADR-002)](./adr/002-Story-Framework-Implementation.md)
- [Component & sequence diagrams](./diagrams/README.md)
- [Authoring skill: `command-query-event-tale`](https://github.com/AdrianStrugala/SolTechnology.Core/blob/master/.github/skills/command-query-event-tale/SKILL.md)

## SolTechnology.Core.Tale

Write multi-step business processes as a `Tale`: a `Tell()` method that reads like a table of
contents, chapters that do one thing each, and an engine that runs them, pauses for user input,
and persists the tale between requests.

[![NuGet](https://img.shields.io/nuget/v/SolTechnology.Core.Tale.svg)](https://www.nuget.org/packages/SolTechnology.Core.Tale/)

### Features

- `Tell()` returns a fluent plan — `Open`, `Read`, `Expect`, `Otherwise`, `WhenLost`, `Do`,
  `Finale` — with no branching code in the handler.
- Chapters are DI-resolved classes that return a `Result` and receive the run's
  `CancellationToken`, so cancelling the run stops in-flight HTTP or database work.
- `InteractiveChapter` pauses the tale and publishes its input schema; `TaleManager` resumes it
  with typed input, across requests and restarts, through a pluggable `ITaleRepository`.
- Every handler is also a plain CQRS handler: send it through `IMediator` when it needs no pause.
- Optimistic concurrency and idempotency keys make concurrent starts, resumes and cancels safe.
- Pause and cancellation are typed errors (`TalePausedError`, `TaleCancelledError`), never strings.
- `TaleController` gives start / resume / cancel / state endpoints for the handlers you opt in.

### Registration

`AddSolTale` scans the given assemblies for chapters and handlers; pass the owning assembly
explicitly.

```csharp
using SolTechnology.Core.Tale;
using SolTechnology.Core.Tale.Builder;

builder.Services.AddSolTale(assemblies: typeof(ProcessOrderTale).Assembly);
```

Choose the persistence provider and the handlers served over HTTP on the returned builder.

```csharp
builder.Services.AddSolTale(assemblies: typeof(ProcessOrderTale).Assembly)
    .UseTaleRepository<MyTaleRepository>(ServiceLifetime.Scoped)
    .ExposeOverHttp<OrderProcessingTale>();
```

### Configuration

| Option | Default | Purpose |
|---|---|---|
| `TaleOptions.TaleIdPrefix` | `"STR"` | Code of the generated `Auid` tale IDs; use one per tenant or environment sharing a store. |
| `TaleOptions.RunningLeaseTimeout` | 5 minutes | How long a `Running` tale belongs to the run that last saved or renewed it; must be positive. See [Running tales and the lease](#running-tales-and-the-lease). |

```csharp
builder.Services.AddSolTale(o => o.TaleIdPrefix = "ORD", typeof(ProcessOrderTale).Assembly);
```

### Usage

#### Write a tale

State flows through the `Context`; each chapter reads it, does one thing, and returns a `Result`.

```csharp
public class OrderInput : IQuery<OrderOutput> { public int OrderId { get; set; } }
public class OrderOutput { public string Status { get; set; } = ""; }

public class OrderContext : Context<OrderInput, OrderOutput>
{
    public decimal Total { get; set; }
    public string CustomerName { get; set; } = "";
}

public class LoadOrder(IOrderRepository orders) : Chapter<OrderContext>
{
    public override async Task<Result> Read(OrderContext context, CancellationToken cancellationToken)
    {
        context.Total = await orders.GetTotal(context.Input.OrderId, cancellationToken);
        return context.Total > 0 ? Result.Success() : Result.Fail("Empty order");
    }
}

public class ProcessOrderTale(IServiceProvider serviceProvider, ILogger<ProcessOrderTale> logger)
    : TaleHandler<OrderInput, OrderContext, OrderOutput>(serviceProvider, logger),
      IQueryHandler<OrderInput, OrderOutput>
{
    protected override Tale<OrderOutput> Tell() =>
        Open<LoadOrder>()
            .Expect(ctx => ctx.Total < 10_000, new ValidationError { Message = "Order needs approval." })
            .Do(ctx => ctx.Output.Status = "Processed")
            .Finale(ctx => ctx.Output);
}
```

`Tell()` runs again on every resume, so it must not branch on the clock, random values, or flags.

#### Run a tale through `IMediator`

A handler that implements `IQueryHandler`/`ICommandHandler` runs like any CQRS handler, with
validation and logging; register its assembly with `AddSolCQRS` too.

```csharp
public class OrdersController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Process([FromBody] OrderInput input, CancellationToken cancellationToken)
        => Ok(await mediator.Send(input, cancellationToken));
}
```

Such a direct run is not persisted. If it pauses on an interactive chapter it returns
`TalePausedError`, logs a warning, and cannot be resumed: start interactive tales through
`TaleManager`.

#### Pause for input

An interactive chapter pauses the tale until `ResumeStory` supplies its input; returning
`Result.Fail` keeps the tale paused so the caller can retry.

```csharp
public class CustomerDetails { public string Name { get; set; } = ""; }

public class RequestCustomerDetails : InteractiveChapter<OrderContext, CustomerDetails>
{
    public override Task<Result> ReadWithInput(
        OrderContext context, CustomerDetails userInput, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userInput.Name))
        {
            return Result.FailAsTask("Customer name is required");
        }

        context.CustomerName = userInput.Name;
        return Result.SuccessAsTask();
    }
}
```

```csharp
public class OrderProcessingTale(IServiceProvider serviceProvider, ILogger<OrderProcessingTale> logger)
    : TaleHandler<OrderInput, OrderContext, OrderOutput>(serviceProvider, logger)
{
    protected override Tale<OrderOutput> Tell() =>
        Open<LoadOrder>()
            .Read<RequestCustomerDetails>()
            .Do(ctx => ctx.Output.Status = $"Confirmed for {ctx.CustomerName}")
            .Finale(ctx => ctx.Output);
}
```

The schema comes from the input type's public properties (`GetRequiredInputSchema()`); input that
does not deserialize keeps the tale paused with a `ValidationError` naming the JSON path.

#### Start, resume and cancel with `TaleManager`

`TaleManager` persists every step, runs each call in its own DI scope, and returns the stored
`TaleInstance`.

```csharp
var start = await taleManager.StartStory<OrderProcessingTale, OrderInput, OrderContext, OrderOutput>(
    new OrderInput { OrderId = 42 }, idempotencyKey: "order-42", cancellationToken);
var taleId = start.Data!.TaleId;                      // Status: WaitingForInput
var fields = start.Data.CurrentChapter!.RequiredData; // render a form from the schema

var input = JsonSerializer.SerializeToElement(new CustomerDetails { Name = "Jan" });
var resume = await taleManager.ResumeStory<OrderProcessingTale, OrderInput, OrderContext, OrderOutput>(
    taleId, input, cancellationToken);                // Status: Completed

var state = await taleManager.GetStoryState(taleId, cancellationToken);
```

| Resume of a tale that is | Result |
|---|---|
| `WaitingForInput` | Runs from the paused chapter; completed chapters are skipped. |
| `Running`, saved or renewed within `RunningLeaseTimeout` | `ConflictError` "Tale is already running". |
| `Running`, not saved or renewed for `RunningLeaseTimeout` | Taken over: runs again from the first unfinished chapter. |
| `Failed` | `ConflictError`; fallbacks do not run again. |
| `Completed` or `Cancelled` | Failure, nothing runs. |
| saved by 1.x | Failure asking to restart the tale. |

A second `StartStory` with the same idempotency key, even a concurrent one, returns the first
tale. `CancelStory(taleId, ct)` wins over a running tale, which stops at its next save or lease
renewal with `ConflictError`. Only the caller's token cancels a tale, and it stores `Cancelled`; any other
`OperationCanceledException`, such as an `HttpClient` timeout, fails the chapter. An exception
from a chapter or an inline step is logged and stores `Failed`; the result and the stored history
carry only "Chapter [X] failed unexpectedly." (or "Tale [X] failed unexpectedly.") and, when a
trace is active, its trace ID as `CorrelationId`; never the exception text.

#### Running tales and the lease

A tale is `Running` while a `StartStory` or `ResumeStory` call executes it. The run saves the tale
before its first chapter and after every chapter, and each save stamps `LastUpdatedAt`. While a
chapter or any other step executes, the run also renews this lease every third of
`RunningLeaseTimeout` with an ordinary `Save` of its last saved state, so a chapter may run longer
than the timeout. While the last save or renewal is younger than `RunningLeaseTimeout`, the tale
belongs to that run and `ResumeStory` returns `ConflictError`. A host that crashes mid-run stops
renewing and leaves the tale `Running`; once the lease has expired, `ResumeStory` takes it over.
Completed chapters are skipped and the chapter that was running when the host died runs again;
when two calls take over at once, the version check lets exactly one win.

A crashed chapter therefore runs twice, so chapters with side effects should still be idempotent:
pass an idempotency key derived from the tale ID to payment or messaging APIs, or check whether the
work is already done. Choose `RunningLeaseTimeout` as how long a crashed tale may stay blocked; it
only has to outlast a host that stalls for two renewals in a row. If a renewal finds that another
run has taken the tale over, the original run cancels the chapter's token, saves nothing more, and
returns `ConflictError`, leaving the new owner's state untouched. Direct `Handle` runs are not
persisted and never renew.

```csharp
builder.Services.AddSolTale(o => o.RunningLeaseTimeout = TimeSpan.FromMinutes(15),
    typeof(ProcessOrderTale).Assembly);
```

#### Guards and recovery

| Step | Runs when | Effect |
|---|---|---|
| `Read<TChapter>()` | the tale is not lost | Runs the chapter; a failure loses the tale. |
| `Expect(condition, error)` | the tale is not lost | Loses the tale with `error` unless `condition` holds. |
| `Otherwise<TChapter>()` / `Otherwise(func)` | the tale is lost | Clears the error and runs the fallback. |
| `WhenLost(effect)` / `WhenWon(effect)` | lost / not lost | Side effect only, such as a log line. |
| `Do(step)` | the tale is not lost | Inline context change that may return a `Result`. |

The first error is the tale's result; `TalePausedError` and `TaleCancelledError` mark a paused and
a cancelled run, so test with `result.Error is TalePausedError`.

#### Expose over HTTP

`TaleController` serves only handlers opted in with `ExposeOverHttp<THandler>()`; every other
name returns a plain 404, and the server logs a warning when the name belongs to a scanned handler.
Reading or cancelling a tale of a handler that is not exposed returns the same 404 as a missing
tale.

```csharp
[Authorize]
[Route("api/orders/tale")]
public class OrderTaleController(
    TaleManager taleManager, TaleHandlerRegistry registry, TaleOptions options, ILogger<TaleController> logger)
    : TaleController(taleManager, registry, options, logger);
```

| Method | Path | Response |
|---|---|---|
| POST | `{route}/{handlerName}/start` | 202 when paused, 200 when completed; honours `Idempotency-Key`. |
| POST | `{route}/{taleId}` | Resume with the JSON body as input; 202 or 200. |
| GET | `{route}/{taleId}` | Current state without the stored context. |
| GET | `{route}/{taleId}/result` | Output of a completed tale. |
| DELETE | `{route}/{taleId}` | Cancel; 404 when the tale is missing. |

Business failures return 400; unexpected errors return 500 with a fixed message and the
correlation ID, never the exception text.

#### Persistence and custom repositories

`UseTaleRepository<T>()` replaces the in-memory default; a repository must honour this contract.

| Member | Contract |
|---|---|
| `Save(instance, ct)` | Write only when the stored version (a missing tale counts as `0`) equals `instance.Version`; then store and set `Version + 1`. Otherwise, or when a new tale reuses another tale's idempotency key, return `Result.Fail(new ConflictError { ... })` without writing. |
| `FindById`, `FindByIdempotencyKey` | Return a copy, or `null` when missing. |
| `LastUpdatedAt` | Store and return it unchanged, in UTC; the running lease is measured from it. |
| `List(...)` | Optional; the default throws `NotSupportedException`. |
| `Delete(id, ct)` | Remove the tale; no error when it is missing. |

Serialize `History` and `CurrentChapter` with `TaleJsonOptions.Default`, which keeps the
`TalePausedError` and `TaleCancelledError` types. The DreamTravel sample's `SQLiteTaleRepository`
is a reference implementation with a `Version` column and a conditional `UPDATE`.

#### Logging

Engine entries use your handler's logger category (for example `MyApp.Orders.ProcessOrderTale`)
inside a scope with `TaleHandler` and `TaleId`; `TaleManager` and `TaleController` log under their
full type names in `SolTechnology.Core.Tale`.

#### Upgrading from 1.x

Tales saved by 1.x cannot be resumed by 2.x, because 2.x matches chapter history by step position
and full type name. Finish or cancel in-flight tales first, then cut every Tale host over in one
step: stop all 1.x hosts before a 2.x host serves tale traffic. Do not roll back to 1.x once a 2.x
host has written tale state; 2.x stores Tale error types, `StepIndex` and `Version` that 1.x cannot
read or preserve. Custom repositories add the `Version` column and the conditional `Save` above.

### Testing

Run tales against the in-memory repository with a real `ServiceCollection`.

```csharp
[Test]
public async Task StartStory_EmptyOrder_FailsAndPersistsFailure()
{
    // Arrange
    var services = new ServiceCollection().AddLogging();
    services.AddSingleton<IOrderRepository>(new FakeOrderRepository(total: 0));
    services.AddSolTale(assemblies: typeof(ProcessOrderTale).Assembly);
    await using var provider = services.BuildServiceProvider();
    var taleManager = provider.GetRequiredService<TaleManager>();

    // Act
    var result = await taleManager.StartStory<ProcessOrderTale, OrderInput, OrderContext, OrderOutput>(
        new OrderInput { OrderId = 1 });

    // Assert
    result.Error!.Message.Should().Be("Empty order");
    var stored = await provider.GetRequiredService<ITaleRepository>().List(TaleStatus.Failed);
    stored.Should().ContainSingle();
}
```

### Conventions

- Keep `Tell()` free of `if`, loops and `try`; branch with `Expect` and `Otherwise`.
- One chapter, one verb, one file; never call one chapter from another.
- Pass the chapter's `CancellationToken` to every awaited call.
- Return `Result.Fail` for business failures; keep secrets and personal data out of the `Context`,
  which is stored as JSON.
- Expose only the interactive workflows a client must drive, behind authorization.

### What ships in DI

| Service | Lifetime | Notes |
|---|---|---|
| Chapters and handlers from the scanned assemblies | Transient | Concrete `IChapter<>` and `TaleHandler<,,>` types. |
| `ITaleRepository` | Singleton | `InMemoryTaleRepository` until `UseTaleRepository<T>()`. |
| `TaleManager` | Scoped | Creates its own scope per call. |
| `TaleHandlerRegistry` | Singleton | Handlers exposed over HTTP. |
| `TaleOptions` | Singleton | Shared by every `AddSolTale` call. |

### Working with AI Agent

The repository ships a **skill** your AI assistant (GitHub Copilot, Claude Code) can read on demand:

- [`command-query-event-tale`](https://github.com/AdrianStrugala/SolTechnology.Core/blob/master/.github/skills/command-query-event-tale/SKILL.md)
  — decide when a handler becomes a Tale, keep `Tell()` logic-free, name chapters one-verb-per-file,
  and choose where the Tale lives (`Commands`/`Queries`, `DomainServices`, or `Workflows`).

It points at the binding rules in the Coding Guide —
[§4 — Tale framework](https://github.com/AdrianStrugala/SolTechnology.Core/blob/master/docs/ClaudeCodingGuide.md)
keeps the anatomy and chapter rules in one place.

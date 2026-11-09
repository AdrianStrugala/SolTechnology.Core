## SolTechnology.Core.Hangfire

Durable CQRS events and typed recurring jobs on top of Hangfire. `AddSolPersistentEvents()` turns every
`IMediator.Publish` into a Hangfire job that survives a process restart, and `AddSolRecurringJob<TJob>()` runs an
`IJob` on a cron schedule; your application keeps ownership of the Hangfire storage, server, and dashboard.

### Features

- **Durable events** — each published `IEvent` is stored as one Hangfire job and dispatched to its handlers in a fresh
  DI scope, so an event published just before a crash still runs.
- **Typed recurring jobs** — an `IJob` returns a `Result`, is resolved from DI per run, and is registered with one line.
- **Stable job ids** — the default id is the job's full type name without assembly version; jobs you stop
  registering are removed at start, without touching other applications that share the storage.
- **Result-aware retry** — a recoverable failed `Result` is retried up to 3 times; a thrown exception is not retried.
- **Overlap guard** — `preventOverlap: true` skips a run while the previous run of the same job is still processing.
- **Correlation** — the caller's correlation id travels with the job and is restored in the job's log scope.
- **Graceful shutdown** — handlers and jobs receive the server's shutdown token; an event cut short is requeued.
- **Upgrade from 1.x in place** — event jobs enqueued by 1.x still run, and 1.x recurring entries are replaced.

### Registration

Register the module, then configure Hangfire with `UseSolFilters` and start a server:

```csharp
builder.Services.AddSolCQRS(o => o.RegisterEventsFromAssembly(typeof(Program).Assembly));
builder.Services.AddSolPersistentEvents();
builder.Services.AddSolRecurringJob<FetchTrafficJob>(Cron.Hourly());

builder.Services.AddHangfire((sp, config) => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString)
    .UseSolFilters(sp));
builder.Services.AddHangfireServer();
```

| Call | Needs | What it does |
|---|---|---|
| `AddSolPersistentEvents()` | `AddSolCQRS()` first | Replaces the in-memory `IEventPublisher` with a Hangfire-backed one |
| `AddSolRecurringJob<TJob>(cron)` | — | Registers `TJob` and adds or updates its recurring entry at host start |
| `UseSolFilters(sp)` | the `AddHangfire` callback | Installs correlation, Smart Retry, and the overlap guard |

The application supplies the rest: storage (`Hangfire.SqlServer`, `Hangfire.InMemory`, …), a server
(`AddHangfireServer()`), and `UseRecommendedSerializerSettings()`, without which an `IEvent` argument cannot be read
back. `AddHangfire` from `Hangfire.NetCore` or `Hangfire.AspNetCore` provides the DI-aware job activator.

### Configuration

| `PersistentEventsOptions` | Default | Purpose |
|---|---|---|
| `QueueName` | `"default"` | Hangfire queue of event jobs; the server must listen on it. |

```csharp
builder.Services.AddSolPersistentEvents(o => o.QueueName = "events");
builder.Services.AddHangfireServer(o => o.Queues = ["events", "default"]);
```

| `AddSolRecurringJob<TJob>` parameter | Default | Purpose |
|---|---|---|
| `cronExpression` | — (required) | Schedule; use Hangfire's `Cron` helpers or a cron string. |
| `preventOverlap` | `false` | Skip a run while another run of this job is processing; needs `UseSolFilters`. |
| `jobId` | full type name of `TJob` | Recurring job id; set it to keep the id when you rename or move the class. |
| `timeZone` | UTC | Time zone the cron expression is evaluated in. |

### Usage

#### Persistent events

Define the event and its handlers in your CQRS layer and publish through `IMediator`; carry ids rather than entities:

```csharp
public sealed record OrderPlaced(int OrderId) : IEvent;

public class SendConfirmationHandler(IEmailSender emailSender) : IEventHandler<OrderPlaced>
{
    public Task Handle(OrderPlaced notification, CancellationToken cancellationToken) =>
        emailSender.SendConfirmation(notification.OrderId, cancellationToken);
}

mediator.Publish(new OrderPlaced(42));
```

`SolTechnology.Core.CQRS` logs and isolates a handler's exception, so a failing handler does not fail the job; the
job guards against losing the event before dispatch, not against a handler's own failure.

#### Recurring jobs

Return a `Result`; mark a transient failure `Recoverable` to have the run retried:

```csharp
public class FetchTrafficJob(ITrafficClient trafficClient) : IJob
{
    public async Task<Result> Execute(CancellationToken cancellationToken)
    {
        var fetched = await trafficClient.FetchLatest(cancellationToken);
        return fetched
            ? Result.Success()
            : Result.Fail(new Error { Message = "Traffic API unavailable", Recoverable = true });
    }
}

builder.Services.AddSolRecurringJob<FetchTrafficJob>("0 */6 * * *", preventOverlap: true);
```

#### Outcomes and retries

| Outcome | Recurring job | Event job |
|---|---|---|
| Success | `Succeeded` | `Succeeded` |
| Failed `Result`, `Recoverable = false` | `Succeeded` | — |
| Failed `Result`, `Recoverable = true` | Retried up to 3 times with Hangfire's back-off, then `Failed` (needs `UseSolFilters`) | — |
| Handler throws | — | Logged by CQRS; job `Succeeded` |
| Job throws | `Failed`, not retried | `Failed`, not retried |
| Server shuts down mid-run | Requeued when the job lets the token's `OperationCanceledException` propagate | Requeued; every handler runs again |

#### Job ids

At start the module adds or updates every registered job, then removes the jobs it registered before and no longer
does; it records its ids in the storage set `sol:recurring-ids:<ApplicationName>`, so services that share one
Hangfire storage never remove each other's jobs.

| Situation | Effect at start |
|---|---|
| Job registered | Entry added or updated under its id |
| Job removed, renamed, or moved to another namespace without `jobId` | Old entry removed, new entry added |
| 1.x entry under the class name (`FetchTrafficJob`) | Removed when it runs the same job; a same-named job of another type is kept |

#### Prevent overlap

`preventOverlap: true` takes a non-blocking storage lock named after the recurring job id for the length of the run;
a run that cannot take it is cancelled and shows as `Deleted`, and a run waiting for a retry does not hold the lock. Without `UseSolFilters` the module
logs `Recurring job [{JobId}] requested preventOverlap but UseSolFilters was not called; overlap is not prevented.` at
start. On any other Hangfire job, apply the filter as an attribute; it locks on the method and its arguments:

```csharp
public class ReportGenerator
{
    [PreventOverlapJobFilter]
    public Task Generate(int reportId) => Task.CompletedTask;
}
```

#### Upgrading from 1.x

| Change | What to do |
|---|---|
| `IJob.Execute` returns `Task<Result>` | Return `Result.Success()`, or a failed `Result` instead of throwing for a transient failure |
| Event jobs use a new method signature | Deploy Hangfire servers (workers) before processes that only enqueue: a 1.x server cannot run event jobs enqueued by 2.x; they fail and are retried until a 2.x server takes them |
| 1.x event jobs in storage | Nothing; they still run. The 1.x signature is removed in 3.0 |
| Recurring ids change to full type names | Nothing; 1.x entries are removed at start. After a rollback to 1.x, delete the 2.x ids (full type names) from the dashboard, or both entries fire |

### Testing

Skip `AddSolPersistentEvents()` in component tests: `AddSolCQRS()` alone dispatches events in memory. Test a job by
calling it:

```csharp
[Test]
public async Task Execute_TrafficApiDown_ReturnsRecoverableFailure()
{
    // Arrange
    var trafficClient = Substitute.For<ITrafficClient>();
    trafficClient.FetchLatest(Arg.Any<CancellationToken>()).Returns(false);
    var sut = new FetchTrafficJob(trafficClient);

    // Act
    var result = await sut.Execute(CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeFalse();
    result.Error!.Recoverable.Should().BeTrue();
}
```

### Conventions

- **Do** make handlers and jobs idempotent; a requeued or retried run executes them again.
- **Do** call `UseSolFilters(sp)`; without it there is no correlation, no Smart Retry, and no overlap guard.
- **Do** return `Recoverable = true` only for transient failures, and pass the `CancellationToken` on.
- **Do** set `jobId` before renaming or moving a recurring job class whose history you want to keep.
- **Do** use `BackgroundJob.Schedule` for one-off delayed work instead of building a delay queue.
- **Don't** put entities in events; they are serialized into storage and may be read long after publishing.
- **Don't** raise `WorkerCount` before measuring; Hangfire's default suits IO-bound handlers.

### What ships in DI

| Service | Implementation | Lifetime |
|---|---|---|
| `IEventPublisher` | Enqueues one Hangfire job per event (`AddSolPersistentEvents`) | Singleton |
| `IHostedService` | Registers recurring jobs and removes stale ones at start | Singleton |
| `TJob` | Your job, one per `AddSolRecurringJob` call | Scoped |
| `IOptions<PersistentEventsOptions>` | Queue name | Singleton |
| `ICorrelationIdService` | From `SolTechnology.Core.Logging` | Singleton |

Background: [background-processing architecture](architecture/background-processing.md), [CQRS](CQRS.md).

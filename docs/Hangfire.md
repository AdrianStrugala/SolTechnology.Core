## SolTechnology.Core.Hangfire

Durable CQRS events and typed recurring jobs on top of Hangfire. `AddSolPersistentEvents()` turns every
`IMediator.Publish` into a Hangfire job that survives a process restart, and `AddSolRecurringJob<TJob>()` runs an
`IJob` on a cron schedule; your application keeps ownership of the Hangfire storage, server, and dashboard.

### Features

- **Durable events** — each published `IEvent` is stored as one Hangfire job and dispatched to its handlers in a fresh
  DI scope, so an event published just before a crash still runs.
- **Typed recurring jobs** — an `IJob` returns a `Result`, is resolved from DI per run, and is registered with one line.
- **Stable job ids** — the default id is the job's full type name without assembly version, shortened when it would
  not fit SQL Server; 1.x entries are replaced at start, and jobs you stop registering can be removed on opt-in.
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
| `jobId` | full type name of `TJob` | Recurring job id of at most 86 characters (a longer one throws `ArgumentException`); set it to keep the id when you rename or move the class. |
| `timeZone` | UTC | Time zone the cron expression is evaluated in. |

| `RecurringJobsOptions` | Default | Purpose |
|---|---|---|
| `RemoveStaleRecurringJobs` | `false` | At start, remove the recurring jobs this application registered before and no longer registers (see Job ids). |

```csharp
builder.Services.Configure<RecurringJobsOptions>(o => o.RemoveStaleRecurringJobs = true);
```

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
| Failed `Result`, `Recoverable = true` | Retried up to 3 times with Hangfire's back-off, then `Failed` (needs `UseSolFilters`; without it the module warns at start, 2151) | — |
| Handler throws | — | Logged by CQRS; job `Succeeded` |
| Job throws | `Failed`, not retried | `Failed`, not retried |
| Server shuts down mid-run | Requeued when the job lets the token's `OperationCanceledException` propagate | Requeued when a handler is interrupted: the remaining handlers are skipped and every handler runs again on the next server; a dispatch that finished first succeeds |

#### Chronicle

`UseSolFilters(sp)` installs a filter that makes every performed job its own
[Chronicle](Log.md#chronicle) root, with attributes `JobId` and `RetryCount`: `Job {TJob}` for a
recurring job, `Event {EventType}` for a persistent event, and `Job {Type}.{Method}` for anything
else. Both installers register Chronicle; without `UseSolFilters` jobs have no root.

| Job outcome | Root status | Hangfire state |
|---|---|---|
| Success | `Ok` | `Succeeded` |
| Failed `Result`, not recoverable | `Failed`, `Recoverable=False` | `Succeeded` |
| Failed `Result`, recoverable | `Failed`, `Recoverable=True` | retried by Smart Retry |
| Exception | `Failed`, with the job's own exception type | `Failed` |
| Server shutdown or abort | `Cancelled`; the rerun gets its own report | requeued |
| Skipped by the overlap guard | no report | `Deleted` |

The filter runs after the correlation, Smart Retry and overlap filters, so the report carries the
job's correlation id. Persistent-event handler steps nest under the event root. Hangfire keeps one
global filter instance per process: with several hosts in one process, the last host that called
`UseSolFilters` reports every job.

#### Job ids

Hangfire.SqlServer stores `recurring-job:<id>` in an `nvarchar(100)` key, so ids are at most 86 characters: a full type
name that is longer, typically a generic job, becomes its type name, `-`, and the first 12 hex characters of the full
name's SHA-256 (``GenericJob`1-ac9290f02c71``), the same in every process and version.

At start the module adds or updates every registered job. Removing a job it no longer registers is opt-in
(`RemoveStaleRecurringJobs`), because the module only knows what the starting instance registers: with the option on,
it records its ids in the storage set `sol:recurring-ids:<ApplicationName>` and removes the recorded ids that this
instance no longer registers.

| Situation | Effect at start |
|---|---|
| Job registered | Entry added or updated under its id |
| Job removed, renamed, or moved to another namespace without `jobId` | Option off: old entry stays until you delete it in the dashboard. Option on: old entry removed |
| Last `AddSolRecurringJob` call removed | Nothing runs at start, so every old entry stays whatever the option; delete them in the dashboard |
| 1.x entry under the class name (`FetchTrafficJob`) | Always removed when it runs the same job; a same-named job of another type is kept |

Turn the option on only when every instance of the application registers the same jobs: an instance started without
a feature-flagged job removes the entry the other instances still run. Two applications that register the same job
type from a shared package share its default id, so the entry follows whichever started last, and with the option on
one removes the other's entry; give each its own `jobId`.

#### Prevent overlap

`preventOverlap: true` takes a non-blocking storage lock named after the recurring job id for the length of the run;
a run that cannot take it is cancelled and shows as `Deleted`, and a run waiting for a retry does not hold the lock.
Without `UseSolFilters` the module logs a warning at start (2150, see Start-up logs). On any other Hangfire job, apply
the filter as an attribute; it locks on the method and its arguments:

```csharp
public class ReportGenerator
{
    [PreventOverlapJobFilter]
    public Task Generate(int reportId) => Task.CompletedTask;
}
```

#### Start-up logs

| Event | EventId | Level | Template |
|---|---|---|---|
| `preventOverlap` without `UseSolFilters` | 2150 | Warning | `Recurring job [{JobId}] requested preventOverlap but UseSolFilters was not called; overlap is not prevented.` |
| Recurring jobs without `UseSolFilters` | 2151 | Warning | `[{JobCount}] recurring jobs are registered but UseSolFilters was not called; a recoverable failed Result is not retried.` |
| Stale job removed (opt-in) | 2152 | Information | `Removed recurring job [{JobId}]: no longer registered by [{ApplicationName}]` |
| 1.x entry removed | 2153 | Information | `Removed 1.x recurring job [{LegacyJobId}]; it runs as [{JobId}]` |

#### Upgrading from 1.x

| Change | What to do |
|---|---|
| `IJob.Execute` returns `Task<Result>` | Return `Result.Success()`, or a failed `Result` instead of throwing for a transient failure |
| Event jobs use a new method signature | Deploy Hangfire servers (workers) before processes that only enqueue: a 1.x server cannot run event jobs enqueued by 2.x; they fail and are retried until a 2.x server takes them |
| 1.x event jobs in storage | Nothing; they still run. The 1.x signature is removed in 3.0 |
| Recurring ids change to full type names | Nothing; 1.x entries are removed at start. After a rollback to 1.x, delete the 2.x ids (full type names) from the dashboard, or both entries fire |
| Rolling back to 1.x | Drain event jobs first: stop the processes that publish and let 2.x servers empty the event queues and scheduled retries. A 1.x server cannot load an event job enqueued by 2.x; such a job fails until a 2.x server runs again, then requeue it from the dashboard |

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
- **Do** set `jobId` before renaming or moving a recurring job class whose history you want to keep, and for a job
  type that several applications share from one package.
- **Do** use `BackgroundJob.Schedule` for one-off delayed work instead of building a delay queue.
- **Don't** put entities in events; they are serialized into storage and may be read long after publishing.
- **Don't** raise `WorkerCount` before measuring; Hangfire's default suits IO-bound handlers.

### What ships in DI

| Service | Implementation | Lifetime |
|---|---|---|
| `IEventPublisher` | Enqueues one Hangfire job per event (`AddSolPersistentEvents`) | Singleton |
| `IHostedService` | Registers recurring jobs and removes 1.x (and, on opt-in, stale) entries at start | Singleton |
| `TJob` | Your job, one per `AddSolRecurringJob` call | Scoped |
| `IOptions<PersistentEventsOptions>` | Queue name | Singleton |
| `IOptions<RecurringJobsOptions>` | Stale-job removal switch | Singleton |
| `ICorrelationIdService` | From `SolTechnology.Core.Logging` | Singleton |

Background: [background-processing architecture](architecture/background-processing.md), [CQRS](CQRS.md).

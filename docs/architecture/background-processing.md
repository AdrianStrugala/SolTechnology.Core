# Background Processing

`SolTechnology.Core.Hangfire` connects CQRS events and recurring jobs to Hangfire while leaving
storage, server, dashboard, activator, and serializer setup to the application. Consumer usage is
in [`docs/Hangfire.md`](../Hangfire.md).

## Persistent events

`AddSolPersistentEvents()` requires `AddSolCQRS()` and replaces `IEventPublisher` with the
internal `HangfireEventPublisher`. It enqueues one job per event, on the configured queue, that
targets `DispatchInScope(IEvent, CancellationToken)`. Hangfire replaces the token placeholder
with the server's shutdown token; the job dispatches all handlers in a fresh scope and carries
`[AutomaticRetry(Attempts = 0)]`. The one-argument `DispatchInScope(IEvent)` stays as a forwarding
overload so jobs enqueued by 1.x still run; it is removed in 3.0. Because 1.x servers cannot load
the two-argument method, Hangfire servers are deployed before processes that only enqueue.

The CQRS dispatcher logs and swallows individual handler exceptions, so a failed handler does not
fail the job: persistent enqueueing protects against process loss before dispatch, not against a
handler's own failure. The exception is cancellation: when the shutdown token is cancelled and a
handler throws `OperationCanceledException`, the dispatcher calls no further handler and the
exception propagates, so Hangfire requeues the job and every handler runs again on the next server
(at-least-once). A dispatch whose handlers all finished before the shutdown succeeds and is not
run again.

## Recurring jobs

`IJob.Execute` returns `Task<Result>`. `AddSolRecurringJob<TJob>()` registers the job (scoped), a
singleton `RecurringJobRunner<TJob>`, and a descriptor. At host start `RecurringJobRegistrar`
adds or updates one recurring entry per descriptor; the runner resolves the job from a fresh
scope and returns its `Result`, which Hangfire writes into the `Succeeded` state.

Job ids and time zone:

- The default id is the version-free full type name (namespace, name, generic arguments, no
  assembly information), so a redeploy keeps it. Hangfire.SqlServer stores
  `recurring-job:<id>` in an `nvarchar(100)` key, so a name over 86 characters becomes the type
  name, `-`, and the first 12 hex characters of the full name's SHA-256. An explicit `jobId` over
  86 characters is rejected at registration.
- The cron expression runs in `timeZone`, UTC by default.

Cleanup at start (`RecurringJobCleanup`):

- Each job's 1.x entry, registered under `Type.Name`, is removed when it is not a current id and
  its stored job runs the same `RecurringJobRunner<TJob>`, so a same-named job of another
  application survives.
- Removing jobs the application no longer registers is opt-in
  (`RecurringJobsOptions.RemoveStaleRecurringJobs`). With it on, the registrar records its ids in
  the storage set `sol:recurring-ids:<ApplicationName>` and removes recorded ids the starting
  instance no longer registers. It is off by default because the registrar only sees the starting
  instance: instances with different feature-flagged jobs, or applications that share a job type
  from one package, would otherwise remove each other's entries.

## Retries

A recurring job reports a transient failure by returning a failed `Result` whose
`Error.Recoverable` is `true`. `SmartRetryJobFilter` turns that outcome into a `FailedState` whose
exception is the internal `RecoverableResultException`, and the runner's
`[AutomaticRetry(Attempts = 3, OnlyOn = [typeof(RecoverableResultException)])]` retries only that
state. The method-level attribute replaces Hangfire's global default of 10 attempts, so a thrown
exception is not retried, as in 1.x, and a non-recoverable failed `Result` ends `Succeeded`.
Returning `Result` keeps jobs on the Coding Guide §13 model and gives Smart Retry something to
inspect (design A6).

## Filters

`UseSolFilters(sp)` installs four pieces, each resolved from the container:

- `CorrelationIdJobFilter` (global): on create it stamps the job parameter `CorrelationId` with
  the ambient id or a new one without writing ambient state, because the recurring scheduler's
  thread is long-lived; on perform it sets the job's id, pushes a log scope, and afterwards
  restores the worker thread's previous id or calls `ICorrelationIdService.Clear()`.
- `SmartRetryJobFilter` (global), described under Retries.
- `ChronicleJobFilter` (global, `Order = 0`): opens a [Chronicle](logging.md#chronicle) root per
  performed job through `BeginRoot` (`Job {TJob}` for a recurring job, `Event {EventType}` for a
  persistent event, `Job {Type}.{Method}` otherwise) and closes it in `OnPerformed` inside a
  `finally`, so a worker thread never keeps a job root as its ambient node. Hangfire sorts filters
  by order and then scope, so order 0 runs it after every default-order filter: the job's
  correlation scope is already pushed, and a run the overlap guard cancels opens no root (its skip
  stays visible as `Deleted`). It implements `IJobFilter` directly to keep its primary
  constructor. The root takes its status from the job's outcome: a failed `Result` is `Failed`
  with `Recoverable`, although Hangfire records `Succeeded` or a Smart Retry; a job exception is
  reported by the inner exception of `JobPerformanceException`; shutdown cancellation and
  `JobAbortedException` are `Cancelled`, and the requeued run gets its own root. `RetryCount` is
  read from the job's parameter snapshot, only while recording. Like the other global filters it
  lives in Hangfire's process-wide collection, so with several hosts in one process the last
  host's instance reports every job.
- `PreventOverlapFilterProvider`, added to Hangfire's process-wide `JobFilterProviders.Providers`
  (replacing one an earlier host in the same process added). It returns `PreventOverlapJobFilter`
  for the runners of jobs registered with `preventOverlap: true`. The filter holds a non-blocking
  distributed storage lock, `sol:overlap:<recurring job id>` (or a hash of method and arguments
  for other jobs), from `OnPerforming` to `OnPerformed`, and cancels a run that cannot take it,
  which Hangfire records as `Deleted`. The check costs the same whatever the storage size, and a
  run waiting in `Scheduled` for a retry does not block the next trigger. Implementing the
  existing filter kept the documented parameter without a breaking change (design A7).

Without `UseSolFilters` the registrar logs Warning 2150 for each `preventOverlap` job and Warning
2151 when recurring jobs are registered, because both features are then silently off. Removals
log Information 2152 (stale) and 2153 (1.x).

## Persisted identity

Hangfire stores type, assembly, method name, and parameter types in every enqueued, scheduled,
and recurring job, and the correlation id under the job parameter `CorrelationId`.
`HangfirePersistedIdentityTests` pins `SolTechnology.Core.Hangfire.HangfireEventPublisher` with
both `DispatchInScope` overloads, ``SolTechnology.Core.Hangfire.RecurringJobRunner`1`` with
`RunAsync(CancellationToken)`, and the parameter key; the Coding Guide §10 `Async` rule does not
apply to `RunAsync`.

The boundary keeps CQRS free of Hangfire dependencies, makes durability opt-in, and leaves
deployment infrastructure under application control.

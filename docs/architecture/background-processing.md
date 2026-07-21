# Background Processing

`SolTechnology.Core.Hangfire` connects CQRS events and recurring jobs to Hangfire while leaving
storage, server, dashboard, activator, and serializer setup to the application.

## Persistent events

`AddSolPersistentEvents()` requires `AddSolCQRS()` and replaces `IEventPublisher`. It enqueues one
Hangfire job per event to the configured queue, then dispatches all handlers in a fresh scope.
The job has `[AutomaticRetry(Attempts = 0)]`.

The CQRS dispatcher logs and swallows individual handler exceptions. Consequently, a failed
handler normally does not mark the Hangfire job as failed and cannot be manually requeued from a
failed-job state. Persistent enqueueing provides process-loss durability before dispatch, not
per-handler failure durability.

## Recurring jobs

`AddSolRecurringJob<TJob>()` registers jobs at host startup with stable ID
`typeof(TJob).Name`; re-registration updates the schedule. `RecurringJobRunner` also disables
automatic retries.

The current `preventOverlap` option is stored but is not consumed by the registrar. It must not be
relied on until implementation and tests make it effective.

`UseSolFilters()` installs correlation and Result-aware state-election filters. It does not
install an overlap-prevention filter.

The boundary keeps CQRS free of Hangfire dependencies, makes durability opt-in, and leaves
deployment infrastructure under application control.

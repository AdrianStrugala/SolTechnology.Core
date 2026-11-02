# Hangfire — `src/SolTechnology.Core.Hangfire/`

- Persisted job identity changed (namespace, type name, assembly name, method name, parameter
  list of `HangfireEventPublisher` or `RecurringJobRunner<TJob>`) → enqueued, scheduled and
  retrying jobs fail with "method not found" after deploy. Pin identities with a test.
- Job parameter key renamed (e.g. the correlation-ID parameter) → in-flight jobs lose state.
- Recurring job ID scheme changed → old and new entries both fire, or the old entry is orphaned;
  any automatic cleanup must not delete IDs owned by another app sharing the storage.
- Retry semantics drift (`[AutomaticRetry]` removed or changed) → thrown exceptions inherit the
  global 10-attempt default and non-idempotent jobs re-run.
- New enqueue signature consumed by an older server during a rolling deploy → the job fails until
  a new server picks it up; deploy servers before enqueuing clients.
- Filter not applied because the consumer never calls `UseSolFilters` → feature silently off.
- Monitoring-API scans (`ScheduledJobs(0, int.MaxValue)`) on the perform path → cost grows with
  the whole storage.

---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 13
status: to-do
---

# Step 13: Hangfire — implement `preventOverlap`; register the correlation service

## Summary
`AddSolRecurringJob<TJob>(cron, preventOverlap: true)` stores the flag in
`RecurringJobDescriptor` and never reads it, so the documented parameter is a no-op (`B5`,
`Hangfire/ModuleInstaller.cs:57,62`, `RecurringJobRegistrar.cs:23-26`). Per design A7, the step
implements it using the existing `Filters/PreventOverlapJobFilter.cs`. The installer also omits
`ICorrelationIdService`, so `UseSolFilters` fails without Logging (`infra#13`).

## Affected components
- `src/SolTechnology.Core.Hangfire/RecurringJobRegistrar.cs` — EDIT — apply the overlap filter per job
- `src/SolTechnology.Core.Hangfire/ModuleInstaller.cs` — EDIT — `UseSolFilters` installs a per-job filter provider; `AddSolCorrelationIdService()`
- `src/SolTechnology.Core.Hangfire/Filters/PreventOverlapJobFilter.cs` — EDIT — only if it needs the descriptor lookup; premortem RM-10: replace the `int.MaxValue` monitoring scans
- `tests/SolTechnology.Core.Hangfire.Tests/Filters/PreventOverlapFilterProviderTests.cs` — NEW — filter-provider unit tests
- `tests/SolTechnology.Core.Hangfire.Tests/ModuleResolutionTests.cs` — EDIT (created in step 11) — remove the infra#13 ignore

## Changes
- Add an internal `IJobFilterProvider` that returns `PreventOverlapJobFilter` for jobs whose `RecurringJobDescriptor.PreventOverlap` is `true`, registered in `UseSolFilters`.
- `services.AddSolCorrelationIdService()` in both `AddSolPersistentEvents` and `AddSolRecurringJob`.
- Tests are deterministic unit tests of the filter provider, not concurrent triggers against a real Hangfire server (flaky):
  - `GetFilters_PreventOverlapTrue_ReturnsPreventOverlapFilter`.
  - `GetFilters_PreventOverlapFalse_ReturnsNoOverlapFilter`.
  - `GetFilters_NonRecurringJob_ReturnsNoOverlapFilter`.
- Premortem RM-10 (step 00, 2026-11-02, scenario 10): enabling the filter must not make every guarded run scan the whole storage.
  - Replace `ScheduledJobs(0, int.MaxValue)`/`ProcessingJobs(0, int.MaxValue)` (`PreventOverlapJobFilter.cs:35,38`) with a check whose cost does not grow with storage size. Example: a non-blocking `IStorageConnection.AcquireDistributedLock("sol:overlap:<recurring job id>", TimeSpan.Zero)` held from `OnPerforming` to `OnPerformed`, cancelling the run when it is taken.
  - `RecurringJobRegistrar` logs a Warning at start, `Recurring job [{JobId}] requested preventOverlap but UseSolFilters was not called; overlap is not prevented.`, when a descriptor has `PreventOverlap = true` and the filter provider is not installed.
  - Tests (Hangfire in-memory storage): `OnPerforming_LockHeld_CancelsRun`, `OnPerformed_ReleasesLock`, `Registrar_PreventOverlapWithoutUseSolFilters_LogsWarning`.

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.Hangfire.Tests` is green; at `4097d47f` no filter provider exists, so `GetFilters_PreventOverlapTrue_ReturnsPreventOverlapFilter` cannot pass there (`B5` regression).
- [ ] `ModuleResolutionTests` has no `[Ignore]`.
- [ ] No public or protected signature changed.
- [ ] RM-10: `grep -n "int.MaxValue" src/SolTechnology.Core.Hangfire/Filters/PreventOverlapJobFilter.cs` prints nothing, and the three RM-10 tests are green.

## Open questions
- none

## Deviations

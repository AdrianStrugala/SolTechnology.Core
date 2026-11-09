---
spec: 2026-11-02-core-2-0-hangfire
step: 02
status: done
---

# Step 02: Hangfire — implement `preventOverlap`; register the correlation service

## Summary
`AddSolRecurringJob<TJob>(cron, preventOverlap: true)` stores the flag in
`RecurringJobDescriptor` and never reads it, so the documented parameter is a no-op (`B5`,
`Hangfire/ModuleInstaller.cs:57,62`, `RecurringJobRegistrar.cs:23-26`). Per design A7, the step
implements it using the existing `Filters/PreventOverlapJobFilter.cs`. The installer also omits
`ICorrelationIdService`, so `UseSolFilters` fails without Logging (`infra#13`).

Former core step 13; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.Hangfire/RecurringJobRegistrar.cs` — EDIT — apply the overlap filter per job
- `src/SolTechnology.Core.Hangfire/ModuleInstaller.cs` — EDIT — `UseSolFilters` installs a per-job filter provider; `AddSolCorrelationIdService()`
- `src/SolTechnology.Core.Hangfire/Filters/PreventOverlapJobFilter.cs` — EDIT — only if it needs the descriptor lookup; premortem RM-10: replace the `int.MaxValue` monitoring scans
- `tests/SolTechnology.Core.Hangfire.Tests/Filters/PreventOverlapFilterProviderTests.cs` — NEW — filter-provider unit tests
- `tests/SolTechnology.Core.Hangfire.Tests/ModuleResolutionTests.cs` — EDIT (created in step 01) — remove the infra#13 ignore

## Changes
- Add an internal `IJobFilterProvider` that returns `PreventOverlapJobFilter` for jobs whose `RecurringJobDescriptor.PreventOverlap` is `true`, registered in `UseSolFilters`.
- `services.AddSolCorrelationIdService()` in both `AddSolPersistentEvents` and `AddSolRecurringJob`.
- Tests are deterministic unit tests of the filter provider, not concurrent triggers against a real Hangfire server (flaky):
  - `GetFilters_PreventOverlapTrue_ReturnsPreventOverlapFilter`.
  - `GetFilters_PreventOverlapFalse_ReturnsNoOverlapFilter`.
  - `GetFilters_NonRecurringJob_ReturnsNoOverlapFilter`.
- Premortem RM-10 (core step 00, 2026-11-02, scenario 10): enabling the filter must not make every guarded run scan the whole storage.
  - Replace `ScheduledJobs(0, int.MaxValue)`/`ProcessingJobs(0, int.MaxValue)` (`PreventOverlapJobFilter.cs:35,38`) with a check whose cost does not grow with storage size. Example: a non-blocking `IStorageConnection.AcquireDistributedLock("sol:overlap:<recurring job id>", TimeSpan.Zero)` held from `OnPerforming` to `OnPerformed`, cancelling the run when it is taken.
  - `RecurringJobRegistrar` logs a Warning at start, `Recurring job [{JobId}] requested preventOverlap but UseSolFilters was not called; overlap is not prevented.`, when a descriptor has `PreventOverlap = true` and the filter provider is not installed.
  - Tests (Hangfire in-memory storage): `OnPerforming_LockHeld_CancelsRun`, `OnPerformed_ReleasesLock`, `Registrar_PreventOverlapWithoutUseSolFilters_LogsWarning`.

## Acceptance criteria
- [x] `dotnet test tests/SolTechnology.Core.Hangfire.Tests` is green; at `4097d47f` no filter provider exists, so `GetFilters_PreventOverlapTrue_ReturnsPreventOverlapFilter` cannot pass there (`B5` regression).
- [x] `ModuleResolutionTests` has no `[Ignore]`.
- [x] No public or protected signature changed.
- [x] RM-10: `grep -n "int.MaxValue" src/SolTechnology.Core.Hangfire/Filters/PreventOverlapJobFilter.cs` prints nothing, and the three RM-10 tests are green.

## Open questions
- none

## Deviations

### Lock key, overlap semantics, and P-25

**Original plan:** Replace the monitoring scans with a check whose cost does not grow with storage size, for example a lock named `sol:overlap:<recurring job id>`.

**Actual implementation:** `PreventOverlapJobFilter` takes `IStorageConnection.AcquireDistributedLock(resource, TimeSpan.Zero)` in `OnPerforming`, keeps the handle in `context.Items`, and disposes it in `OnPerformed`; a `DistributedLockTimeoutException` cancels the run. The resource is `sol:overlap:` plus the job's `RecurringJobId` parameter (set by Hangfire on every recurring trigger), or, for a job without one (the filter used as an attribute), a SHA-256 hash of the serialized type, method, parameter types and arguments, which keeps the 1.x "same method and arguments" scope. The lock sees only processing runs, so a run waiting in `Scheduled` for a retry no longer blocks the next trigger; this behaviour change of the public filter is recorded as P-25 in the core and feature briefs.

### Filter provider installation

**Original plan:** `UseSolFilters` installs a per-job filter provider.

**Actual implementation:** `PreventOverlapFilterProvider` (singleton, built from the descriptors) is added to Hangfire's process-wide `JobFilterProviders.Providers`, which `AddHangfire` exposes to the server as `IJobFilterProvider`. `InstallInto` replaces a provider an earlier host added (test hosts, `WebApplicationFactory`), so stale descriptors never double-apply the filter, and sets `IsInstalled`, which `RecurringJobRegistrar` reads to log the RM-10 warning. `UseSolFilters_PreventOverlapJob_ServerSeesOverlapFilter` checks the wiring through a real `AddHangfire` container.

### Test layout

**Original plan:** New `Filters/PreventOverlapFilterProviderTests.cs`; RM-10 tests named as listed.

**Actual implementation:** The monitoring-API tests in `Filters/PreventOverlapJobFilterTests.cs` are replaced by in-memory-storage lock tests (`OnPerforming_LockHeld_CancelsRun`, `OnPerformed_ReleasesLock`, plus same/different-invocation tests); the registrar tests move from `RecurringJobTests.cs` to a new `RecurringJobRegistrarTests.cs` (with `Registrar_PreventOverlapWithoutUseSolFilters_LogsWarning`), because the registrar gained a logger and the overlap provider. `AddHangfire` points Hangfire's static log provider at the container's `ILoggerFactory`, so a disposed test provider broke later `InMemoryStorage` construction; the test-only `HangfireGlobalState.Reset` undoes the static log provider, global filters and filter provider after each test that builds an `AddHangfire` container.

Verification: `dotnet test tests/SolTechnology.Core.Hangfire.Tests -c Release` 39/39 green, no `[Ignore]`.

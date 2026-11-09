---
spec: 2026-11-02-core-2-0-hangfire
step: 04
status: done
---

# Step 04: Hangfire — recurring job contract for 2.0

## Summary
Two recurring-job contracts need to change:

- Smart Retry only reacts to a returned `Result`, but `IJob.Execute` returns `Task`, and the docs
  describe a `[SmartRetry]` attribute that does not exist (`infra#10`).
- Recurring job IDs are `Type.Name`, so same-named jobs overwrite each other, stale IDs are never
  removed, and there is no time-zone option (`infra#23`).

The ID change alters persisted recurring entries (P-06), and the `IJob` change is `C-19`. The
event-dispatch signature (P-05) is in step 03. Jobs that 1.x registered under `Type.Name` IDs are
not in the new `sol:recurring-ids` set, so without extra work both the old and the new entry would
fire after the upgrade. RQ10 (user, 2026-11-02): the registrar removes legacy `Type.Name` IDs
automatically, with a test.

Former core step 51; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.Hangfire/IJob.cs` — EDIT — `Task<Result> Execute(CancellationToken cancellationToken)` (`C-19`, confirmed 2026-11-02)
- `src/SolTechnology.Core.Hangfire/RecurringJobRunner.cs` — EDIT — return the job's `Result`; remove `[AutomaticRetry(Attempts = 0)]` only if Smart Retry now owns retries
- `src/SolTechnology.Core.Hangfire/RecurringJobRegistrar.cs` — EDIT — `:21` ID `jobId ?? <version-free full type name of TJob>` (premortem RM-3); `RecurringJobOptions { TimeZone = timeZone ?? TimeZoneInfo.Utc }`; `RemoveIfExists` for registered IDs no longer present (tracked under a per-application `sol:recurring-ids:<ApplicationName>` set in storage, premortem RM-8); `RemoveIfExists(descriptor.JobType.Name)` for each descriptor whose legacy 1.x ID differs from its new ID and is not the ID of any current descriptor
- `src/SolTechnology.Core.Hangfire/RecurringJobDescriptor.cs` — EDIT — `JobId`, `TimeZone`
- `src/SolTechnology.Core.Hangfire/ModuleInstaller.cs` — EDIT — `AddSolRecurringJob<TJob>(string cronExpression, bool preventOverlap = false, string? jobId = null, TimeZoneInfo? timeZone = null)`
- `sample-tale-code-apps/DreamTravel/src/Presentation/DreamTravel.Worker/**` and every `IJob` implementation (`grep -rln ": IJob" sample-tale-code-apps src tests`) — EDIT
- `tests/SolTechnology.Core.Hangfire.Tests/RecurringJobTests.cs`, `Filters/SmartRetryJobFilterTests.cs` — EDIT

## Changes
- Tests (Hangfire in-memory storage):
  - `RecurringJob_RecoverableResult_IsRetriedBySmartRetry`.
  - `AddSolRecurringJob_SameNameDifferentNamespace_TwoJobs`.
  - `Registrar_RemovedJob_IsDeletedFromStorage`.
  - `Registrar_Legacy1xTypeNameId_IsRemovedOnStart` (storage seeded with a recurring job under `typeof(TestJob).Name`; after start only the `FullName` entry exists).
  - `Registrar_LegacyIdEqualToCurrentJobId_IsKept`.
- Premortem mitigations (core step 00, 2026-11-02):
  - RM-3 (scenario 3): the default ID is a version-free full type name (namespace + name, generic arguments without assembly information), never `Type.FullName` of a generic. Test `AddSolRecurringJob_GenericJobType_IdHasNoAssemblyVersion`.
  - RM-7 (scenario 7): keep `RecurringJobRunner<TJob>`'s namespace, type name, assembly, method name `RunAsync`, and parameter list `(CancellationToken)`. Only the return type changes (Hangfire matches by name and parameters). Do not rename `RunAsync` for Guide §10; Hangfire stores the name in every job. `HangfirePersistedIdentityTests` (step 03) stays green; only a return-type assertion may be added.
  - RM-8 (scenario 8):
    - Scope the tracked set per application (`IHostEnvironment.ApplicationName`), and remove only IDs recorded in this application's set, so services that share one Hangfire storage never delete each other's jobs. Test `Registrar_TwoApplicationsSharedStorage_KeepEachOthersJobs`.
    - Add to the core brief's "Release-notes inputs": "After a rollback to 1.x, delete the 2.x recurring IDs (full type names) from the Hangfire dashboard, or both the old and the new entry fire."
  - RM-9 (scenario 9):
    - A recurring job that throws keeps 1.x semantics: no automatic retry. Only a returned recoverable `Result` is retried, with an explicit, bounded attempt count set on the runner, not Hangfire's global default of 10.
    - Tests: `RecurringJob_Throws_IsNotRetried`, `RecurringJob_RecoverableResult_RetriedAtMostConfiguredTimes`.
    - If Hangfire's filter pipeline cannot separate the two, record a deviation, add a `P-nn` row to the core brief stating the exception retry count, and keep that count explicit on the runner.
  - Step 06 carries the `docs/Hangfire.md` notes for RM-8 and RM-9.

## Acceptance criteria
- [x] `dotnet test tests/SolTechnology.Core.Hangfire.Tests` is green.
- [x] The DreamTravel Worker builds and starts in the component tests.
- [x] Premortem mitigations for `ModuleInstaller.cs` and the persisted contracts are applied.
- [x] The core brief's P-06 row states that legacy IDs are removed automatically; release step 05 ([`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md)) copies it into `docs/migration-2.0.md`.
- [x] RM-3: `AddSolRecurringJob_GenericJobType_IdHasNoAssemblyVersion` is green.
- [x] RM-7: `HangfirePersistedIdentityTests` is green, and `grep -n "RunAsync" src/SolTechnology.Core.Hangfire/RecurringJobRunner.cs` still matches.
- [x] RM-8: `Registrar_TwoApplicationsSharedStorage_KeepEachOthersJobs` is green, and the core brief's "Release-notes inputs" contains the rollback note.
- [x] RM-9: `RecurringJob_Throws_IsNotRetried` and `RecurringJob_RecoverableResult_RetriedAtMostConfiguredTimes` are green, or a deviation and a P-row record the explicit exception retry count.

## Open questions
- none (`C-19` confirmed by the user on 2026-11-02; RQ10 resolved: automatic legacy-ID removal)

## Deviations

### Retry budget, retry filter and exception type

**Original plan:** Only a returned recoverable `Result` is retried, with an explicit, bounded attempt count on the runner; remove `[AutomaticRetry(Attempts = 0)]` only if Smart Retry now owns retries.

**Actual implementation:** `RecurringJobRunner<TJob>.RunAsync` carries
`[AutomaticRetry(Attempts = RecurringJobRetry.RecoverableResultAttempts, OnlyOn = [typeof(RecoverableResultException)])]`
with the internal constant set to 3 (the plan names no number). `SmartRetryJobFilter` now elects a `FailedState`
whose exception is the new internal `RecoverableResultException` (derived from `InvalidOperationException`, the type it
used before), so Hangfire's own retry filter retries only those; a thrown exception does not match `OnlyOn` and stays
`Failed`, as in 1.x. The method-level attribute replaces Hangfire's global default of 10 because `AutomaticRetry`
allows one instance per job. No P-row was needed; the release-notes inputs gain a `hangfire 04` note on the retry
semantics and the dashboard exception type.

### Legacy-id removal checks the stored job type; tracked-id cleanup in its own class

**Original plan:** `RemoveIfExists(descriptor.JobType.Name)` for each descriptor whose legacy id differs from its new id and is not the id of any current descriptor.

**Actual implementation:** The legacy entry is removed only when its stored job is this job's
`RecurringJobRunner<TJob>`, so another application's 1.x job that shares the short type name survives
(`Registrar_LegacyIdOfAnotherJobType_IsKept`). The tracked-set and legacy cleanup lives in a new internal
`RecurringJobCleanup`, which logs each removal at Information (`Removed recurring job [{JobId}]: no longer registered
by [{ApplicationName}]`, `Removed 1.x recurring job [{LegacyJobId}]; it runs as [{JobId}]`), so `RecurringJobRegistrar`
stays within the Guide §9 budgets. The registrar now also takes `JobStorage` and `IHostEnvironment`, which every
`AddHangfire` host provides. The version-free type name is a new internal `JobTypeName.Of`, the same algorithm as
Tale's `ChapterTypeName` and Cache's `CacheKey.TypeName`. `AddSolRecurringJob` rejects a whitespace `jobId`.

### Test layout

**Original plan:** Tests extend `RecurringJobTests.cs` and `Filters/SmartRetryJobFilterTests.cs`.

**Actual implementation:** The storage-backed id tests live in `RecurringJobIdTests.cs` and
`RecurringJobLegacyIdTests.cs` over a shared test helper `RecurringJobHost` (in-memory storage, a real
`RecurringJobManager`, one `IHostEnvironment.ApplicationName` per start); the retry tests live in
`RecurringJobRetryTests.cs`, which performs runner jobs with Hangfire's `BackgroundJobPerformer` and elects their state
with `BackgroundJobStateChanger`, with `SmartRetryJobFilter` and Hangfire's default `AutomaticRetryAttribute` as global
filters. Test job types moved to `TestJobs.cs`. Extra tests: `AddSolRecurringJob_CustomIdAndTimeZone_StoredOnEntry`,
`Registrar_LegacyIdOfAnotherJobType_IsKept`, `AddSolRecurringJob_WhitespaceJobId_Throws`.
`HangfirePersistedIdentityTests` gains only the `Task<Result>` return-type assertion that RM-7 allows.

Verification: `dotnet test tests/SolTechnology.Core.Hangfire.Tests -c Release` 60/60 green; removing `OnlyOn` makes
`RecurringJob_Throws_IsNotRetried` fail (the throw is scheduled for a retry). `dotnet build SolTechnology.Core.slnx -c
Release` succeeds (only the pre-existing DreamTravel sample warnings), and `DreamTravel.Component.Tests`, which start
the Worker, are 31/31 green.

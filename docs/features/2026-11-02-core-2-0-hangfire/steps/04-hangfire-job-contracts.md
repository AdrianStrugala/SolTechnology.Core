---
spec: 2026-11-02-core-2-0-hangfire
step: 04
status: to-do
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
- [ ] `dotnet test tests/SolTechnology.Core.Hangfire.Tests` is green.
- [ ] The DreamTravel Worker builds and starts in the component tests.
- [ ] Premortem mitigations for `ModuleInstaller.cs` and the persisted contracts are applied.
- [ ] The core brief's P-06 row states that legacy IDs are removed automatically; release step 05 ([`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md)) copies it into `docs/migration-2.0.md`.
- [ ] RM-3: `AddSolRecurringJob_GenericJobType_IdHasNoAssemblyVersion` is green.
- [ ] RM-7: `HangfirePersistedIdentityTests` is green, and `grep -n "RunAsync" src/SolTechnology.Core.Hangfire/RecurringJobRunner.cs` still matches.
- [ ] RM-8: `Registrar_TwoApplicationsSharedStorage_KeepEachOthersJobs` is green, and the core brief's "Release-notes inputs" contains the rollback note.
- [ ] RM-9: `RecurringJob_Throws_IsNotRetried` and `RecurringJob_RecoverableResult_RetriedAtMostConfiguredTimes` are green, or a deviation and a P-row record the explicit exception retry count.

## Open questions
- none (`C-19` confirmed by the user on 2026-11-02; RQ10 resolved: automatic legacy-ID removal)

## Deviations

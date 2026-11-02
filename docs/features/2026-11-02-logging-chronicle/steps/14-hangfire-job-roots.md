---
spec: 2026-11-02-logging-chronicle
step: 14
status: to-do
---

# Step 14: Hangfire job roots — recurring jobs and persistent events

## Summary
Every Hangfire job performed on a server becomes its own Chronicle root through an internal
server filter that calls `BeginRoot` (B11) in `OnPerforming` and closes the root in
`OnPerformed`. The filter runs after `CorrelationIdJobFilter`, so the root's entries carry the
restored correlation. Recurring jobs are named after the `IJob` type and persistent events after
the event type, so reports read like the work instead of `RecurringJobRunner<TJob>.RunAsync`.
`UseSolFilters` installs the filter, and both Hangfire installers register Chronicle (CP-03).

## Affected components
- `src/SolTechnology.Core.Hangfire/Filters/ChronicleJobFilter.cs` — NEW — `internal sealed class ChronicleJobFilter(IChronicle chronicle) : JobFilterAttribute, IServerFilter`; `Order` greater than `CorrelationIdJobFilter.Order`
- `src/SolTechnology.Core.Hangfire/ModuleInstaller.cs` — EDIT — `AddSolPersistentEvents` and `AddSolRecurringJob<TJob>` call `services.AddSolChronicle()` and `services.TryAddSingleton<ChronicleJobFilter>()`; `UseSolFilters` adds the filter when it resolves
- `tests/SolTechnology.Core.Hangfire.Tests/SolTechnology.Core.Hangfire.Tests.csproj` — EDIT — `PackageReference Microsoft.Extensions.Diagnostics.Testing` (same version as step 04)
- `tests/SolTechnology.Core.Hangfire.Tests/Filters/ChronicleJobFilterTests.cs` — NEW — `PerformingContext`/`PerformedContext` built as in `Filters/CorrelationIdJobFilterTests.cs`
- `tests/SolTechnology.Core.Hangfire.Tests/ModuleInstallerTests.cs` — EDIT

## Changes
- Root name:
  - `RecurringJobRunner<TJob>` → `Job {TJob.Name}`.
  - `HangfireEventPublisher.DispatchInScope` → `Event {eventType.Name}` (from the first job argument).
  - Anything else → `Job {Job.Type.Name}.{Job.Method.Name}`.
- Root attributes: `JobId`, `RetryCount` (`GetJobParameter<int>("RetryCount")`). Job arguments are never recorded.
- The root handle is stored in `filterContext.Items["ChronicleRoot"]`. The ambient state follows the capture-and-restore pattern of `2026-11-02-core-2-0-hangfire` step "Hangfire — restore ambient correlation state; async, cancellable event dispatch".
- Outcome mapping in `OnPerformed`:
  - `Exception` set and not `ExceptionHandled` → `Fail(exception)`.
  - `Canceled` → `Cancel()`.
  - Otherwise `Ok`.
- Event handler steps from step 10 nest under the `Event {EventType}` root, because `DispatchInScope` runs inside it.
- Tests (`FakeLogCollector`; tree assertions on `FakeLogRecord.Message`):
  - `OnPerformed_JobSucceeded_EmitsOneRootReportOk`.
  - `OnPerformed_JobThrew_RootFailedWithException`.
  - `OnPerformed_Canceled_RootCancelled`.
  - `OnPerforming_RecurringRunner_RootNamedAfterJobType`.
  - `OnPerforming_PersistentEvent_RootNamedAfterEventType`.
  - `JobBody_ChronicleSteps_NestUnderJobRoot`, run through a real `Hangfire.Server.BackgroundJobPerformer` with a `JobFilterCollection` holding `CorrelationIdJobFilter` and `ChronicleJobFilter` and an async job method, so the `AsyncLocal` flow from `OnPerforming` into the job body and the restore in `OnPerformed` are proven against Hangfire's real filter pipeline, not only against hand-built contexts.
  - `OnPerforming_CanceledByEarlierFilter_StartsNoRoot` (for example `preventOverlap`).
  - `OnPerforming_InsideOpenRoot_StartsSeparateRoot`.
  - `OnPerformed_RestoresPreviousAmbientNode`.
  - `ChronicleJobFilter_Order_RunsAfterCorrelationFilter`.
  - `UseSolFilters_AddsChronicleFilter`.
  - `AddSolRecurringJob_RegistersChronicle`.
  - `AddSolPersistentEvents_RegistersChronicle`.
- Premortem mitigations (step 00, 2026-11-02):
  - CRM-2: `OnPerformed` disposes the root handle from `filterContext.Items` inside a `finally`,
    so the root closes and the ambient node is restored on every `OnPerformed` path, including the
    exception path and a failure raised by a later filter. A Hangfire worker thread must never keep
    a job root as its ambient node after the perform.
  - Tests through a real `BackgroundJobPerformer`:
    `OnPerforming_LaterFilterThrows_RootEmittedFailedAndAmbientRestored` (a test filter ordered
    after `ChronicleJobFilter` throws in `OnPerforming`) and
    `SequentialJobsOnSameThread_SecondReportHasNoStepsOfFirst` (two jobs performed on one thread;
    after each, `IChronicle.IsActive` is `false`).

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.Hangfire.Tests` is green.
- [ ] `dotnet build SolTechnology.Core.slnx` is green, and `cd sample-tale-code-apps/DreamTravel && dotnet build` is green.
- [ ] Premortem mitigations for `ModuleInstaller.cs` (step 00) are applied.
- [ ] CRM-2: `OnPerforming_LaterFilterThrows_RootEmittedFailedAndAmbientRestored` and `SequentialJobsOnSameThread_SecondReportHasNoStepsOfFirst` are green.

## Open questions
- none

## Deviations

---
spec: 2026-11-02-logging-chronicle
step: 14
status: done
---

# Step 14: Hangfire job roots — recurring jobs and persistent events

## Summary
Every Hangfire job performed on a server becomes its own Chronicle root through an internal
server filter that calls `BeginRoot` (B11) in `OnPerforming` and closes the root in
`OnPerformed`. The filter has an explicit `Order = 0`, so it runs after every default-order
filter: `CorrelationIdJobFilter` (the root's entries carry the restored correlation),
`SmartRetryJobFilter`, and the method-scoped `PreventOverlapJobFilter` (a run cancelled by the
overlap guard never opens a root). The root status comes from the job's outcome: a failed
`Result` returned by `IJob.Execute` is `Failed` even though Hangfire records `Succeeded`. Recurring
jobs are named after the `IJob` type and persistent events after the event type, so reports read
like the work instead of `RecurringJobRunner<TJob>.RunAsync`. `UseSolFilters` installs the filter,
and both Hangfire installers register Chronicle (CP-03).

## Affected components
- `src/SolTechnology.Core.Hangfire/Filters/ChronicleJobFilter.cs` — NEW — `internal sealed class ChronicleJobFilter(IChronicle chronicle) : JobFilterAttribute, IServerFilter`; sets `Order = 0` in its constructor (decision R1)
- `src/SolTechnology.Core.Hangfire/ModuleInstaller.cs` — EDIT — the private `AddSolJobFilters()` member (shared by `AddSolPersistentEvents` and `AddSolRecurringJob<TJob>`) calls `services.AddSolChronicle()` and `services.TryAddSingleton<ChronicleJobFilter>()`; `UseSolFilters` adds the filter through `configuration.UseFilter` when it resolves, next to `CorrelationIdJobFilter` and `SmartRetryJobFilter`; stays ≤ 150 lines (130 at `c30d8998`)
- `tests/SolTechnology.Core.Hangfire.Tests/SolTechnology.Core.Hangfire.Tests.csproj` — EDIT — `PackageReference Microsoft.Extensions.Diagnostics.Testing` (version from `Directory.Packages.props`, added by step 04)
- `tests/SolTechnology.Core.Hangfire.Tests/Filters/ChronicleJobFilterTests.cs` — NEW — `PerformingContext`/`PerformedContext` built as in `Filters/CorrelationIdJobFilterTests.cs`; real-performer tests reuse the harness of `RecurringJobRetryTests.cs`/`HangfireEventShutdownTests.cs` and reset process-wide state through `HangfireGlobalState.Reset`
- `tests/SolTechnology.Core.Hangfire.Tests/ModuleInstallerTests.cs` — EDIT

## Changes
- Root name:
  - `RecurringJobRunner<TJob>` → `Job {TJob.Name}`.
  - `HangfireEventPublisher.DispatchInScope` (both the `(IEvent, CancellationToken)` and the legacy `(IEvent)` overload, P-05) → `Event {eventType.Name}` (from the first job argument).
  - Anything else → `Job {Job.Type.Name}.{Job.Method.Name}`.
- Root attributes: `JobId`, `RetryCount` (`GetJobParameter<int>("RetryCount")`). Job arguments are never recorded.
- Filter order (decision R1, verified against Hangfire 1.8.23 `JobFilterProviderCollection`: filters
  sort by `Order`, then by scope, `Global` before `Method`):
  - `CorrelationIdJobFilter`, `SmartRetryJobFilter`, and `PreventOverlapJobFilter` keep the default
    `Order = -1`, so their `OnPerforming` runs before the Chronicle filter's, and their
    `OnPerformed` runs after it.
  - A run cancelled by the overlap guard (`filterContext.Canceled = true`, state `Deleted`) never
    reaches `ChronicleJobFilter.OnPerforming`, so it opens no root and emits no 2154 entry. The
    skip stays visible as the `Deleted` state, as documented in `docs/Hangfire.md`.
  - The same holds for any other default-order filter that cancels a run (for example a
    `[PreventOverlapJobFilter]` attribute on an arbitrary job method).
- The root handle is stored in `filterContext.Items["ChronicleRoot"]`. The ambient state follows
  the capture-and-restore pattern of `CorrelationIdJobFilter`: `BeginRoot` captures the previous
  ambient node, and disposing the root restores it; when there was none, the ambient node becomes
  `null` again, mirroring `ICorrelationIdService.Clear()` (P-27).
- Outcome mapping in `OnPerformed` (decision R2; Hangfire 1.8.23 `CoreBackgroundJobPerformer`
  wraps job exceptions in `JobPerformanceException` and rethrows shutdown cancellation as is):
  - `Exception` is `OperationCanceledException` and `filterContext.CancellationToken.ShutdownToken`
    is cancelled, or `Exception` is `JobAbortedException` → `Cancel()`. This is the P-26 path: the
    CQRS dispatcher rethrows the handler's cancellation, Hangfire requeues the event job, and the
    rerun produces its own report.
  - Any other `Exception` → `Fail(exception.InnerException)` when it is a `JobPerformanceException`
    with an inner exception, otherwise `Fail(exception)`, so the report names the job's own error
    type.
  - `filterContext.Canceled` (only a filter ordered after the Chronicle filter can cause it) →
    `Cancel()` with attribute `Outcome=CanceledByFilter`.
  - `filterContext.Result is Result { IsFailure: true } failed` →
    `Fail(failed.Error?.GetType().Name ?? nameof(Error), failed.Error?.Message)` and attribute
    `Recoverable=true|false`. `IJob.Execute` returns `Task<Result>` (`C-19`) and a failed `Result` is
    returned, not thrown, so Hangfire records `Succeeded` for a non-recoverable failure and
    Smart Retry turns a recoverable one into a retry. Both are `Failed` in the report. `Error` has no
    code member: the error code is the `Error` runtime type name, recorded with `Error.Message` only
    (the RM-15 rule that CQRS 2149 follows).
  - Otherwise `Ok`.
- Event handler steps from step 10 nest under the `Event {EventType}` root, because
  `DispatchInScope` runs inside it. On shutdown the cancelled handler's step is `Cancelled` and the
  remaining handlers get no step (P-26, step 10).
- Process-wide filters: `UseFilter` writes to Hangfire's static `GlobalJobFilters`, and Hangfire
  keeps one instance per filter type, the last one added. With several hosts in one process (the
  DreamTravel component tests start the API and the Worker), a job root may belong to another
  host's recorder. This is the same as `CorrelationIdJobFilter`; re-check at implementation against
  CRM-1 (premortem addendum PA-5).
- Tests (`FakeLogCollector`; tree assertions on `FakeLogRecord.Message`):
  - `OnPerformed_JobSucceeded_EmitsOneRootReportOk`.
  - `OnPerformed_JobThrew_RootFailedWithInnerExceptionType` (a `JobPerformanceException` wrapping
    `InvalidOperationException` → `Error=[InvalidOperationException: …]`).
  - `OnPerformed_NonRecoverableFailedResult_RootFailedWithErrorTypeAndRecoverableFalse`.
  - `OnPerformed_RecoverableFailedResult_RootFailedWithRecoverableTrue`.
  - `OnPerformed_ShutdownCancellation_RootCancelled`.
  - `OnPerformed_JobAborted_RootCancelled`.
  - `OnPerformed_CanceledByLaterFilter_RootCancelledWithOutcome`.
  - `OnPerforming_RecurringRunner_RootNamedAfterJobType`.
  - `OnPerforming_PersistentEvent_RootNamedAfterEventType` (both `DispatchInScope` overloads).
  - `JobBody_ChronicleSteps_NestUnderJobRoot`, run through a real `Hangfire.Server.BackgroundJobPerformer` with a `JobFilterCollection` holding `CorrelationIdJobFilter` and `ChronicleJobFilter` and an async job method, so the `AsyncLocal` flow from `OnPerforming` into the job body and the restore in `OnPerformed` are proven against Hangfire's real filter pipeline, not only against hand-built contexts.
  - `RecurringJob_FailedResult_ThroughRealPerformer_RootFailed` (the runner returns a failed
    `Result`; Hangfire state `Succeeded`; one Error 2154 entry with `ChronicleStatus = Failed`).
  - `OverlapGuardCancels_ThroughRealPerformer_StartsNoRootAndEmitsNothing` (a real performer over
    `Hangfire.InMemory` with `PreventOverlapFilterProvider` installed and the overlap lock held:
    `Canceled`, no 2154 entry, `IChronicle.IsActive` is `false` afterwards).
  - `EventDispatch_ShutdownCancellation_ThroughRealPerformer_RootCancelledAndHandlerStepCancelled`
    (as `HangfireEventShutdownTests`).
  - `OnPerforming_InsideOpenRoot_StartsSeparateRoot`.
  - `OnPerformed_RestoresPreviousAmbientNode`.
  - `ChronicleJobFilter_Order_IsZeroAndRunsAfterCorrelationAndOverlapFilters`.
  - `UseSolFilters_AddsChronicleFilter`.
  - `AddSolRecurringJob_RegistersChronicle`.
  - `AddSolPersistentEvents_RegistersChronicle`.
- Premortem mitigations (step 00, 2026-11-02):
  - CRM-2: `OnPerformed` disposes the root handle from `filterContext.Items` inside a `finally`,
    so the root closes and the ambient node is restored on every `OnPerformed` path, including the
    exception path and a failure raised by a later filter. A Hangfire worker thread must never keep
    a job root as its ambient node after the perform.
  - Tests through a real `BackgroundJobPerformer`:
    `OnPerforming_LaterFilterThrows_RootEmittedFailedAndAmbientRestored` (a test filter with
    `Order = 1`, so it runs after `ChronicleJobFilter`, throws in `OnPerforming`; re-check at
    implementation, decision R1 changed the filter order) and
    `SequentialJobsOnSameThread_SecondReportHasNoStepsOfFirst` (two jobs performed on one thread;
    after each, `IChronicle.IsActive` is `false`).
- Premortem addendum (2026-11-09, brief "Resync with the shipped 2.0 libraries"): PA-1 (failed
  `Result` reported `Ok`), PA-2 (overlap skips reported as cancelled roots), PA-3 (shutdown requeue
  reported `Failed`, wrapper exception type), and PA-5 (process-wide filters) are mitigated by the
  outcome mapping, the filter order, and the tests above.

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.Hangfire.Tests` is green.
- [ ] `dotnet build SolTechnology.Core.slnx` is green, and `cd sample-tale-code-apps/DreamTravel && dotnet build` is green.
- [ ] `wc -l < src/SolTechnology.Core.Hangfire/ModuleInstaller.cs` prints ≤ 150.
- [ ] Premortem mitigations for `ModuleInstaller.cs` (step 00) are applied.
- [ ] CRM-2: `OnPerforming_LaterFilterThrows_RootEmittedFailedAndAmbientRestored` and `SequentialJobsOnSameThread_SecondReportHasNoStepsOfFirst` are green.
- [ ] PA-1: `OnPerformed_NonRecoverableFailedResult_RootFailedWithErrorTypeAndRecoverableFalse` and `RecurringJob_FailedResult_ThroughRealPerformer_RootFailed` are green.
- [ ] PA-2: `OverlapGuardCancels_ThroughRealPerformer_StartsNoRootAndEmitsNothing` and `ChronicleJobFilter_Order_IsZeroAndRunsAfterCorrelationAndOverlapFilters` are green.
- [ ] PA-3: `OnPerformed_ShutdownCancellation_RootCancelled`, `OnPerformed_JobThrew_RootFailedWithInnerExceptionType`, and `EventDispatch_ShutdownCancellation_ThroughRealPerformer_RootCancelledAndHandlerStepCancelled` are green.

## Open questions
- none

## Deviations

### The filter implements `IJobFilter` instead of deriving from `JobFilterAttribute`

**Original plan:** `internal sealed class ChronicleJobFilter(IChronicle chronicle) : JobFilterAttribute, IServerFilter`,
setting `Order = 0` in its constructor.

**Actual implementation:** `JobFilterAttribute.Order` is a settable, non-virtual property, and a
primary constructor has no body to set it in, so the filter implements `IServerFilter` and
`IJobFilter` directly (`Order => 0`, `AllowMultiple => false`) and keeps the primary constructor
(Guide §9.5). Hangfire takes a global filter's order from `IJobFilter.Order`:
`ChronicleJobFilter_Order_IsZeroAndRunsAfterCorrelationAndOverlapFilters` sorts it after
`CorrelationIdJobFilter`, `SmartRetryJobFilter` and the method-scoped `PreventOverlapJobFilter`, and
`AddSolHangfire_RegistersChronicleAndUseSolFiltersAddsItsFilter` finds it in `GlobalJobFilters`
with order 0. The root naming lives in the internal `ChronicleJobName`. `OnPerforming` reads
`RetryCount` before opening the root, because Hangfire skips `OnPerformed` for a filter whose
`OnPerforming` threw, and an open root would then stay ambient on the worker thread.

### Test layout

- Hand-built contexts: `ChronicleJobFilterTests` (outcomes) and `ChronicleJobFilterRootTests`
  (naming, nesting, order). Real `BackgroundJobPerformer`: `ChronicleJobFilterPerformerTests`
  (job body, sequential jobs, later filter throws) and `ChronicleJobFilterPerformerOutcomeTests`
  (failed `Result` with Hangfire state `Succeeded`, overlap guard over `Hangfire.InMemory`, event
  shutdown). Shared code is in `ChronicleJobFilterTestBase`, `ChronicleJobPerformerTestBase` and
  `ChronicleJobFixtures.cs`; the fixture event handlers are internal so `ModuleResolutionTests`
  does not scan them.
- Merged by parameterization: the recoverable and non-recoverable failed `Result` tests
  (`OnPerformed_FailedResult_RootFailedWithErrorTypeAndRecoverable`), shutdown and abort
  (`OnPerformed_ShutdownOrAbort_RootCancelled`), the root-name tests including both
  `DispatchInScope` overloads (`OnPerforming_JobKind_RootNamedAfterTheWork`), and
  `UseSolFilters_AddsChronicleFilter` with both registration tests
  (`AddSolHangfire_RegistersChronicleAndUseSolFiltersAddsItsFilter`).
  `OnPerformed_RestoresPreviousAmbientNode` is folded into
  `OnPerforming_InsideOpenRoot_StartsSeparateRootAndRestoresIt`, which also checks `DetachedFrom`.
- `HangfireGlobalState.Reset` also removes the `ChronicleJobFilter`.

### CRM-2 re-check at implementation (2026-11-10)

Holds. `OnPerformed` disposes the stored root in a `finally`, and R1 moved the CRM-2 test filter to
`Order = 1` as planned: `OnPerforming_LaterFilterThrows_RootEmittedFailedAndAmbientRestored` (one
`Failed` 2154 entry, `IChronicle.IsActive` false afterwards) and
`SequentialJobsOnSameThread_SecondReportHasNoStepsOfFirst` are green through the real performer.

### PA-5 (the Hangfire half of CRM-1) re-check at implementation (2026-11-10)

Accepted residual, same exposure as `CorrelationIdJobFilter`. `UseFilter` adds the instance to the
static `GlobalJobFilters`; with `AllowMultiple = false` Hangfire keeps one instance per type, the
last one added, so with several hosts in one process every job root belongs to the last host's
recorder and is reported through its logger factory. Steps from the other host's services still
join the ambient root (they write into the node, not into their own recorder), and SQL commands are
recorded once, by the observer whose recorder owns the root (CRM-1, step 07). A disposed host's
filter stays installed until the process ends; its reporter never throws into a job (listener
failures are caught by `ChronicleWriter`). Tests remove the filter through
`HangfireGlobalState.Reset`. Step 15 documents the one-host-per-process assumption in `docs/Hangfire.md`.


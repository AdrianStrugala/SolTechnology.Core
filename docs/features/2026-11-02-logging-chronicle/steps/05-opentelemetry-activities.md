---
spec: 2026-11-02-logging-chronicle
step: 05
status: to-do
---

# Step 05: Chronicle steps as OpenTelemetry Activities on `OperationsName`

## Summary
Every step started through `IChronicle` also starts an `Activity` on the existing
`CoreLoggingActivitySources.OperationsName` source. This happens only when that source has
listeners and `EmitActivities` is true, so hosts without OpenTelemetry pay one
`HasListeners()` check (B7). Activities are created even when Chronicle recording is disabled or
no root is active, so callers that move their own Activity onto Chronicle (CQRS, step 10) keep
their spans. Attributes become tags. Status maps to `ActivityStatusCode`.

## Affected components
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleActivities.cs` — NEW — internal: start/stop helpers over the internal `CoreLoggingActivitySources.Operations` instance (internal since `2026-11-02-core-2-0-logging` step "Logging — public surface cleanup"); `ActivityKind.Internal`
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleStep.cs` — EDIT — the handle carries `Activity?`; `Set`/`SetMasked` also `SetTag` (masked value only); `Fail` → `SetStatus(Error, message)` + `AddException`; `Cancel` → tag `chronicle.status=cancelled` (status left `Unset`); `Dispose` stops the activity
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleRecorder.cs` — EDIT — start the activity in `Begin`/`Step`; internal `BeginWithoutActivity(name)` for roots whose host already has an activity (used by step 09)
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleOptions.cs` — EDIT — `EmitActivities` (`bool`, `true`) (CH-04)
- `src/SolTechnology.Core.Logging/Operations/CoreLoggingActivitySources.cs` — EDIT — XML doc of `OperationsName` names Chronicle steps; no signature change
- `tests/SolTechnology.Core.Logging.Tests/Chronicles/ChronicleActivityTests.cs` — NEW

## Changes
- The activity name is the step name. The root report carries `TraceId` from `Activity.Current`
  at root start. Since the 2.0 logging work (`CorrelationId.FromRequest`, logging code review L1) a
  request's `CorrelationId` equals the Activity trace id only when the request carried trace
  context; otherwise it is the inbound `X-Correlation-Id`. The report therefore keeps `TraceId` as
  its own field, and the correlation id reaches the entry through the log scope.
- Tests (an `ActivityListener` filtered on `OperationsName` with `AllDataAndRecorded`):
  - `Step_WithListener_StartsActivityWithParentChain`.
  - `Step_WithoutListener_StartsNoActivity`.
  - `Step_ChronicleDisabledWithListener_StillStartsActivity`.
  - `EmitActivitiesFalse_StartsNoActivity`.
  - `SetMasked_TagHoldsMaskedValueOnly`.
  - `Fail_SetsErrorStatusAndException`.
  - `Cancel_TagsCancelledWithUnsetStatus`.
  - `Report_CarriesTraceIdOfAmbientActivity`.
- `Step_ChronicleDisabled_AllocatesNothing` (step 01) still runs without a listener and still
  measures 0 bytes.
- Premortem mitigations (step 00, 2026-11-02):
  - CRM-10: inside an active root, only recorded nodes start Activities. Overflow handles
    (beyond `MaxSteps`) and depth-dropped handles (beyond `MaxDepth`) start none, so spans per root
    are bounded by `MaxSteps`. Test `MaxSteps_Exceeded_StartsNoActivityForDroppedSteps`
    (`MaxSteps = 10`, 50 steps → at most 10 Activities from the root).

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.Logging.Tests` is green.
- [ ] `grep -rn "new ActivitySource" src/SolTechnology.Core.Logging` lists only `Operations/CoreLoggingActivitySources.cs`.
- [ ] CRM-10: `MaxSteps_Exceeded_StartsNoActivityForDroppedSteps` is green.

## Open questions
- none (OQ-1 confirmed by the user 2026-11-02: `CH-04` `EmitActivities`)

## Deviations

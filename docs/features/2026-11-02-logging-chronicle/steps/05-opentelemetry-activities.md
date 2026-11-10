---
spec: 2026-11-02-logging-chronicle
step: 05
status: done
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
- [x] `dotnet test tests/SolTechnology.Core.Logging.Tests` is green.
- [x] `grep -rn "new ActivitySource" src/SolTechnology.Core.Logging` lists only `Operations/CoreLoggingActivitySources.cs`.
- [x] CRM-10: `MaxSteps_Exceeded_StartsNoActivityForDroppedSteps` is green.

## Open questions
- none (OQ-1 confirmed by the user 2026-11-02: `CH-04` `EmitActivities`)

## Deviations

### Activity details the plan left open

**Original plan:** `Fail` → `SetStatus(Error, message)` + `AddException`; the report carries the
trace id of `Activity.Current` at root start.

**Actual implementation:** `Fail` also tags `error.type` with the exception type name or error
code, and `Fail(errorType)` without a message uses the error type as the status description. The
report's `TraceId` is set only when `Activity.Current` uses the W3C id format (a hierarchical id
has no trace id). The activity name is the step name after the `MaxAttributeValueLength` cut, and
a `Step` outside a root (or with Chronicle disabled) returns an activity-only handle when the
source has listeners.


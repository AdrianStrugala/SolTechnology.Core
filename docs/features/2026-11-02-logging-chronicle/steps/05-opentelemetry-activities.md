---
spec: 2026-11-02-logging-chronicle
step: 05
status: blocked
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
  at root start.
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

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.Logging.Tests` is green.
- [ ] `grep -rn "new ActivitySource" src/SolTechnology.Core.Logging` lists only `Operations/CoreLoggingActivitySources.cs`.

## Open questions
- OQ-1 (brief): `CLAUDE.md §2` confirmation of `CH-04` (`EmitActivities`).

## Deviations

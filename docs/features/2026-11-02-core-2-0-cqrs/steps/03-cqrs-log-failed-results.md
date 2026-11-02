---
spec: 2026-11-02-core-2-0-cqrs
step: 03
status: to-do
---

# Step 03: CQRS — log failed `Result`s as failures

## Summary
When a handler returns `Result.Fail`, `LoggingPipelineBehavior` logs `OperationSucceeded` and
sets the activity status to `Ok` (`cqrs-tale#11`,
`CQRS/PipelineBehaviors/LoggingPipelineBehavior.cs:55-57`). Nothing tests this behaviour,
including `[LogScope]` and masking (`cqrs-tale#26`). The step changes the log contract (L-01),
which is accepted for 2.0.

Former core step 24; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.CQRS/PipelineBehaviors/LoggingPipelineBehavior.cs` — EDIT — failure branch
- `src/SolTechnology.Core.Logging/` — EDIT only if a new `[LoggerMessage]` `OperationFailedWithResult` source-generated method is needed (Logging owns operation lifecycle shapes, Guide §11). New EventId; no existing EventId changes.
- `tests/SolTechnology.Core.CQRS.Tests/PipelineBehaviors/LoggingPipelineBehaviorTests.cs` — NEW

## Changes
- When `result is Result { IsFailure: true } r`, log a Warning with `[{ErrorType}]` and `[{ErrorMessage}]` and set `Activity.SetStatus(ActivityStatusCode.Error)`.
- Tests:
  - `Handle_ResultFail_LogsFailureAndErrorStatus`.
  - `Handle_ResultSuccess_LogsSucceeded`.
  - `Handle_LogScopeWithMaskedProperty_MasksValue`.
- Premortem RM-15 (core step 00, 2026-11-02, scenario 15): the failure entry carries only the `Error` runtime type name and `Error.Message`. It never logs `ValidationError` detail collections, `AggregateError.InnerErrors` messages, or `Result<T>.Data`. Test: `Handle_ValidationResultFail_LogsTypeAndMessageOnly`, asserting that no structured property holds the detail list or `Data`. The PII note for consumers goes into `docs/Log.md` in step 05.

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.CQRS.Tests` is green.
- [ ] The core brief's L-01 entry matches the delivered behaviour; release step 05 ([`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md)) copies it into the 2.0 release notes.
- [ ] RM-15: `Handle_ValidationResultFail_LogsTypeAndMessageOnly` is green.

## Open questions
- none

## Deviations

---
spec: 2026-11-02-core-2-0-cqrs
step: 03
status: done
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
- [x] `dotnet test tests/SolTechnology.Core.CQRS.Tests` is green.
- [x] The core brief's L-01 entry matches the delivered behaviour; release step 05 ([`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md)) copies it into the 2.0 release notes.
- [x] RM-15: `Handle_ValidationResultFail_LogsTypeAndMessageOnly` is green.

## Open questions
- none

## Deviations

### `LoggingPipelineBehaviorTests.cs` extended, not created

**Original plan:** `tests/SolTechnology.Core.CQRS.Tests/PipelineBehaviors/LoggingPipelineBehaviorTests.cs` — NEW.

**Actual implementation:** logging step 05 had already created the file with the
`OperationsName` listener test. The four planned tests were added to it; the activity listener
moved into `SetUp`/`TearDown` so every test can assert the activity status, and log entries are
captured with the `RecordingLoggerProvider` added in step 02. `Handle_ResultFail_*` and
`Handle_ValidationResultFail_*` failed before the fix; the success and masking tests pin existing
behaviour.

### Failure emitter lives in CQRS, not in Logging

**Original plan:** edit `src/SolTechnology.Core.Logging/` only if a new `[LoggerMessage]`
`OperationFailedWithResult` method is needed.

**Actual implementation:** Logging's `OperationLogMessages` is internal and CQRS has no
`InternalsVisibleTo`, so a Logging-owned emitter would need a new public `LoggerExtensions` member —
a public API change this feature does not have (no `C-nn` covers it). The source-generated emitter
is the internal `CQRS/PipelineBehaviors/OperationResultLogMessages.cs`: EventId 2149
`OperationFailedWithResult`, Warning, template
`Operation: [{OperationName}]. Status: [FAIL]. Duration: [{DurationMs} ms]. ErrorType: [{ErrorType}]. ErrorMessage: [{ErrorMessage}]`,
so queries on `Status: [FAIL]` match both failed results and exceptions. The id continues Logging's
2137–2148 range. Logging is unchanged. The activity status is `Error` without a description, and a
failed `Result` without an `Error` logs `ErrorType` `Error`.

### RM-15 and `AggregateError.Message`

**Original plan:** the entry never logs `AggregateError.InnerErrors` messages.

**Actual implementation:** no structured property holds an error object, detail collection, or
data (asserted by `Handle_ValidationResultFail_LogsTypeAndMessageOnly`). `AggregateError.Message`,
however, is built from the inner messages by design (`SolTechnology.Core/Errors/AggregateError.cs`),
so logging `Error.Message` includes them for that type. The step 05 `docs/Log.md` note covers it:
no `Error.Message`, including an aggregate's inner messages, may contain PII.

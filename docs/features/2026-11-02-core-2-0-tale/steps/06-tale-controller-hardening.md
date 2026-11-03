---
spec: 2026-11-02-core-2-0-tale
step: 06
status: done
---

# Step 06: Tale controller — consistent JSON options, no internal detail in responses

## Summary
`TaleController` has two problems:

- It deserializes the start input with default, case-sensitive options, while resume uses
  `TaleJsonOptions` and `/result` drops `IncludeFields`. Typical camelCase JSON therefore binds
  to nulls (`cqrs-tale#14`, `Api/TaleController.cs:60,196`).
- A resume payload that fails to deserialize terminally fails the tale and stores
  `ex.ToString()` in `ChapterInfo.Error`, which `GET /api/tale/{id}` then returns. 500 responses
  also echo `ex.Message` (`cqrs-tale#15`, `TaleEngine.cs:338-343`,
  `TaleController.cs:84,89,136,141,210`, `TaleManager.cs:181`).

Former core step 27; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.Tale/Api/TaleController.cs` — EDIT — `TaleJsonOptions.Default` at every (de)serialization; generic 500 body carrying the correlation ID
- `src/SolTechnology.Core.Tale/Orchestration/TaleEngine.cs` — EDIT — `:338-343` a deserialization failure becomes `PauseForRetry` with a validation-style error, no stack trace
- `src/SolTechnology.Core.Tale/Orchestration/TaleManager.cs` — EDIT — `:181` no `ex.Message` in the returned error
- `tests/SolTechnology.Core.Tale.Tests/TaleControllerTests.cs` — EDIT

## Changes
- Tests:
  - `Start_CamelCaseInput_Binds`.
  - `Resume_InvalidPayload_PausesForRetry_AndNoStackTraceInGet`.
  - `Start_HandlerThrows_500BodyHasNoExceptionMessage`.

## Acceptance criteria
- [x] `dotnet test tests/SolTechnology.Core.Tale.Tests` is green.
- [x] `GET /api/tale/{id}` JSON in tests contains no `"   at "` stack frames.

## Open questions
- none

## Deviations

### The 500 test needs a repository failure; a throwing handler yields a 400

**Original plan:** Test `Start_HandlerThrows_500BodyHasNoExceptionMessage`.

**Actual implementation:** A handler that throws (here, in its constructor) never reaches the
controller's 500 path: `TaleManager` catches the exception and returns a failed `Result`, which the
controller maps to 400. So two tests cover the two leaks: `Start_HandlerThrows_BodyHasNoExceptionMessage`
(400 body without the exception text, through the `TaleManager` fix) and
`Start_RepositoryThrows_500BodyHasNoExceptionMessage_AndCarriesCorrelationId` (an idempotency
lookup that throws reaches the controller's catch; the body is a fixed message plus the correlation
ID). All four new tests failed on `master` before the change (`40a3c5b8`).

### Correlation ID source, null payloads, and `GetStoryState`

**Original plan:** Generic 500 body carrying the correlation ID; a deserialization failure becomes
`PauseForRetry`; no `ex.Message` at `TaleManager.cs:181`.

**Actual implementation:** Tale does not reference `SolTechnology.Core.Logging`, so the controller
takes the correlation ID from `Activity.Current` (the W3C trace ID that Logging's
`ICorrelationIdService` also aligns with) and falls back to `HttpContext.TraceIdentifier`. The
rejected input becomes a `ValidationError` naming the JSON path, and a `null` payload is rejected
the same way instead of failing the tale. `TaleManager.GetStoryState` had the same `ex.Message`
leak and now returns a fixed message too.

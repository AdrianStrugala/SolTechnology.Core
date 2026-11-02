---
spec: 2026-11-02-core-2-0-tale
step: 06
status: to-do
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
- [ ] `dotnet test tests/SolTechnology.Core.Tale.Tests` is green.
- [ ] `GET /api/tale/{id}` JSON in tests contains no `"   at "` stack frames.

## Open questions
- none

## Deviations

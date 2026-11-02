---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 25
status: to-do
---

# Step 25: Tale engine — caller-only cancellation, no resume of failed tales, deterministic tests

## Summary
Two engine state-machine defects, plus weak tests that hide them:

- Any `OperationCanceledException`, including an `HttpClient` timeout inside a chapter, marks the
  tale `Cancelled` and persists that terminally (`cqrs-tale#4`, `Orchestration/TaleEngine.cs:275`,
  `TaleHandler.cs:172`).
- `ResumeStory` on a `Failed` tale returns **Success**, re-runs `Otherwise`, and makes `WhenLost`
  throw on `_errors[^1]` (`cqrs-tale#6`, `TaleManager.cs:114-117`, `TaleEngine.cs:465,177`).
- `AdvancedScenariosTests.Concurrent_ResumeAttempts` asserts `success + failure == 5`, which can
  never fail (`cqrs-tale#26`).

## Affected components
- `src/SolTechnology.Core.Tale/Orchestration/TaleEngine.cs` — EDIT — `catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)`; other cancellations become a chapter error
- `src/SolTechnology.Core.Tale/TaleHandler.cs` — EDIT — `:172` same filter
- `src/SolTechnology.Core.Tale/Orchestration/TaleManager.cs` — EDIT — reject `TaleStatus.Failed` in `ExecuteTalePipeline` with `Result.Fail(new ConflictError(...))`
- `tests/SolTechnology.Core.Tale.Tests/AdvancedScenariosTests.cs` — EDIT — `:436-469,224-246,324-337` deterministic assertions; remove `#region`s in the touched file
- `tests/SolTechnology.Core.Tale.Tests/PauseResumeIntegrationTests.cs` — EDIT — `Resume_FailedTale_ReturnsConflict`
- `tests/SolTechnology.Core.Tale.Tests/ErrorHandlingTests.cs` — EDIT — `Chapter_HttpTimeout_IsChapterErrorNotCancellation`

## Changes
- Delete the tautological `Concurrent_ResumeAttempts` here. An exact "one resume wins, the rest get `ConflictError`" test needs optimistic concurrency, so step 41 adds it.
- Replace the "either/or" and `NotBeNull`-only assertions in `AdvancedScenariosTests.cs:224-246,324-337` with exact status and result assertions.

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.Tale.Tests` is green, and the two negative tests fail on `4097d47f`.
- [ ] `grep -rn "success + failure" tests/SolTechnology.Core.Tale.Tests` prints nothing.

## Open questions
- none

## Deviations

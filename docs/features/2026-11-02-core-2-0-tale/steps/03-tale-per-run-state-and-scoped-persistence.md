---
spec: 2026-11-02-core-2-0-tale
step: 03
status: done
---

# Step 03: Tale — per-run state isolation; persist only `TaleManager`-driven runs

## Summary
`AddSolTale` always registers `InMemoryTaleRepository`, and the engine serializes every run after
each chapter, including plain CQRS query and command tales. The result is a singleton with
unbounded growth that retains PII (`B8`, `Tale/ModuleInstaller.cs:108`,
`Orchestration/TaleEngine.cs:98-105,288,439`). A second `Handle` on the same handler instance
reuses the old `TaleInstanceId`, reloads the completed history, and returns the first result
(`B9`, `TaleHandler.cs:145`). The two fixes share one internal channel from `TaleManager` to the
handler, so they ship together (design A2(a), P-02).

Former core step 18; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.Tale/TaleHandler.cs` — EDIT — fresh context per `Handle` unless an internal resume context is passed
- `src/SolTechnology.Core.Tale/Orchestration/TaleEngine.cs` — EDIT — persistence only when the run is manager-driven
- `src/SolTechnology.Core.Tale/Orchestration/TaleManager.cs` — EDIT — passes the internal run options (persist and resume context)
- `tests/SolTechnology.Core.Tale.Tests/TaleHandlerTests.cs` — EDIT
- `tests/SolTechnology.Core.Tale.Tests/InMemoryRepositoryTests.cs` — EDIT

## Changes
- Internal `TaleRunOptions { bool Persist; TaleInstance? Resume; }` passed by `TaleManager`. The public `Handle(input, ct)` uses `Persist = false` and a new context.
- Tests:
  - `Handle_TwiceOnSameInstance_ReturnsSecondResult`.
  - `Handle_Direct_DoesNotWriteRepository` (repository count stays 0 after 100 runs).
  - `StartStory_ViaManager_Persists`.
- Premortem RM-17 (core step 00, 2026-11-02, scenario 17): when a non-persisted run (`Persist = false`) pauses on an interactive chapter, the engine logs a Warning, `Tale [{TaleHandler}] paused outside TaleManager; the run is not persisted and cannot be resumed.`. The returned result is unchanged. Test: `Handle_Direct_InteractiveChapterPauses_LogsNotPersistedWarning`.
- `docs/Tale.md:369-385` is corrected in step 13.

## Acceptance criteria
- [x] `dotnet test tests/SolTechnology.Core.Tale.Tests` is green, and the two new negative tests fail on `4097d47f`.
- [x] `dotnet test sample-tale-code-apps/DreamTravel/tests/Component/DreamTravel.Component.Tests.csproj` is green.
- [x] No public or protected signature changed.
- [x] RM-17: `Handle_Direct_InteractiveChapterPauses_LogsNotPersistedWarning` is green.

## Open questions
- none

## Deviations

### Negative tests checked against the pre-change `master`, not `4097d47f`

**Original plan:** The two new negative tests fail on `4097d47f`.

**Actual implementation:** They were run against `master` immediately before this change
(`41633d08`) and failed there (`Handle_TwiceOnSameInstance_ReturnsSecondResult` returned
`Result: 30`; `Handle_Direct_DoesNotWriteRepository` found persisted runs). The `TaleHandler`,
`TaleEngine`, and `TaleManager` paths involved are unchanged since `4097d47f` apart from the
namespace import and step 02's serializer options, so the result carries over.

### Resume state comes from `TaleManager`, and every run gets a `TaleId`

**Original plan:** Internal `TaleRunOptions { bool Persist; TaleInstance? Resume; }`.

**Actual implementation:** As planned; the engine now takes the history from `Resume` instead of
reading the repository a second time. A direct run always gets a new `TaleId` (used by the log
scope and `TalePausedError`), where 1.x assigned one only when a repository was registered —
which `AddSolTale` always did.

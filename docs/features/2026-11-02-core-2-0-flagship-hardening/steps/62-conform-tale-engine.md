---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 62
status: to-do
---

# Step 62: Guide conformance — Tale engine split

## Summary
`TaleEngine.cs` has 509 lines and three types, far over the 150-line hard cap (`cqrs-tale#22`).
It also hand-writes constructor fields and has unbraced `if`s and essay comments
(`cqrs-tale#23,25`). This step splits only the engine, as a behaviour-preserving refactor under
the tests added in phases D–F. The rest of Tale follows in step 63 (split from the former single
Tale conformance step during plan review, RQ11). Use the
[`refactor`](../../../../.github/skills/refactor/SKILL.md) skill.

## Affected components
- `src/SolTechnology.Core.Tale/Orchestration/TaleEngine.cs` — EDIT — keeps the orchestration loop only; primary constructor; braces
- `src/SolTechnology.Core.Tale/Orchestration/TaleStepInterpreter.cs` — NEW — internal
- `src/SolTechnology.Core.Tale/Orchestration/TaleStatePersistence.cs` — NEW — internal
- `src/SolTechnology.Core.Tale/Orchestration/InteractiveChapterInvoker.cs` — NEW — internal, from `InteractiveChapterReflection`

## Changes
- `TaleJsonOptions` already lives in `Persistence/TaleJsonOptions.cs` (step 17); nothing to extract for it here.
- No public or protected signature change.
- No log-template change (that is step 70).
- Premortem RM-16 (step 00, 2026-11-02, scenario 16): the extraction into `TaleStatePersistence` must not touch serializer options, the history key, or the `Version` handling. These guard tests keep their names and assertions:
  - `RoundTrip_TalePausedError_WithTaleJsonOptions_PreservesType`, `Clone_ChapterWithTalePausedError_PreservesErrorType` (step 17);
  - `Save_StaleVersion_ReturnsConflict`, `ConcurrentResume_OneWins_OthersConflict` (step 41);
  - `Resume_1xHistory_FailsWithDescriptiveError`, `HistoryKey_GenericChapter_HasNoAssemblyVersion` (step 42);
  - the `4097d47f` Tale JSON test (step 44).

## Acceptance criteria
- [ ] `find src/SolTechnology.Core.Tale/Orchestration -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` lists no `TaleEngine`, `TaleStepInterpreter`, `TaleStatePersistence`, or `InteractiveChapterInvoker` file (`TaleManager.cs` is step 63).
- [ ] `dotnet test tests/SolTechnology.Core.Tale.Tests` is green, and the DreamTravel component tests are green.
- [ ] Public API diff is empty (compare `dotnet build` ref assemblies, or use `Microsoft.DotNet.ApiCompat` locally if available).
- [ ] RM-16: `git diff --stat -- tests/` for this step prints nothing; the guard tests above are green.

## Open questions
- none

## Deviations

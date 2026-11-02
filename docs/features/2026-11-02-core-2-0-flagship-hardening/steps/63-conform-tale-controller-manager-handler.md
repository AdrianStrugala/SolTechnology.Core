---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 63
status: to-do
---

# Step 63: Guide conformance — Tale controller, manager, handler, `Tale`, and tests

## Summary
After step 62, four Tale files still exceed the 150-line hard cap: `TaleController.cs` (331),
`TaleManager.cs` (209), `TaleHandler.cs` (196), and `Tale.cs` (162) (`cqrs-tale#22`). The listed
classes hand-write constructor fields and use `static class` + `this` (`cqrs-tale#23`). They also
have unbraced `if`s and essay comments (`cqrs-tale#25`), and nine Tale test files use `#region`
(`cqrs-tale#26`). This is a behaviour-preserving refactor; use the
[`refactor`](../../../../.github/skills/refactor/SKILL.md) skill.

## Affected components
- `src/SolTechnology.Core.Tale/Api/TaleController.cs` — EDIT — DTOs moved to `Api/TaleControllerModels.cs` (NEW)
- `src/SolTechnology.Core.Tale/Orchestration/TaleManager.cs`, `src/SolTechnology.Core.Tale/TaleHandler.cs`, `src/SolTechnology.Core.Tale/Tale.cs` — EDIT — under 150 lines; primary constructors
- `src/SolTechnology.Core.Tale/ModuleInstaller.cs`, `src/SolTechnology.Core.Tale/Builder/TaleBuilderExtensions.cs`, `src/SolTechnology.Core.Tale/Models/DataField.cs` — EDIT — `extension` blocks
- `tests/SolTechnology.Core.Tale.Tests/*.cs` — EDIT — remove `#region`

## Changes
- No public or protected signature change. Extension blocks keep the containing static class name (binary-compatible).
- No log-template change (that is step 70).
- Premortem RM-16 (step 00, 2026-11-02, scenarios 16 and 23):
  - Moving `AddSolTale` into an `extension` block keeps `[MethodImpl(MethodImplOptions.NoInlining)]` and the `Assembly.GetCallingAssembly()` capture from step 26 on the method the consumer calls.
  - These guard tests keep their names and assertions; the only allowed diff in their files is `#region` removal:
    - `AddSolTale_NoArgs_ScansCallerAssembly` (step 26);
    - the step 42 history tests;
    - `Start_NonExposedHandler_Returns404`, `Start_ScannedButNotExposed_Returns404AndLogsWarning` (step 43);
    - the step 17 Tale-error round-trip tests;
    - `Handle_Direct_InteractiveChapterPauses_LogsNotPersistedWarning` (step 18).

## Acceptance criteria
- [ ] `find src/SolTechnology.Core.Tale -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [ ] `grep -rn "#region" tests/SolTechnology.Core.Tale.Tests` prints nothing.
- [ ] `dotnet test tests/SolTechnology.Core.Tale.Tests` is green, and the DreamTravel component tests are green.
- [ ] Public API diff is empty.
- [ ] Premortem mitigations for `ModuleInstaller.cs` are applied.
- [ ] RM-16: `grep -n "NoInlining" src/SolTechnology.Core.Tale/ModuleInstaller.cs` matches; `git diff` of the guard tests' files shows only `#region`/`#endregion` lines removed; the guard tests are green.

## Open questions
- none

## Deviations

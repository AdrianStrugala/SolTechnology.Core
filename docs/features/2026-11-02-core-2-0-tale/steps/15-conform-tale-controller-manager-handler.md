---
spec: 2026-11-02-core-2-0-tale
step: 15
status: done
---

# Step 15: Guide conformance — Tale controller, manager, handler, `Tale`, and tests

## Summary
After step 14, four Tale files still exceed the 150-line hard cap: `TaleController.cs` (331),
`TaleManager.cs` (209), `TaleHandler.cs` (196), and `Tale.cs` (162) (`cqrs-tale#22`). The listed
classes hand-write constructor fields and use `static class` + `this` (`cqrs-tale#23`). They also
have unbraced `if`s and essay comments (`cqrs-tale#25`), and nine Tale test files use `#region`
(`cqrs-tale#26`). This is a behaviour-preserving refactor; use the
[`refactor`](../../../../.github/skills/refactor/SKILL.md) skill.

Former core step 63; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.Tale/Api/TaleController.cs` — EDIT — DTOs moved to `Api/TaleControllerModels.cs` (NEW)
- `src/SolTechnology.Core.Tale/Orchestration/TaleManager.cs`, `src/SolTechnology.Core.Tale/TaleHandler.cs`, `src/SolTechnology.Core.Tale/Tale.cs` — EDIT — under 150 lines; primary constructors
- `src/SolTechnology.Core.Tale/ModuleInstaller.cs`, `src/SolTechnology.Core.Tale/Builder/TaleBuilderExtensions.cs`, `src/SolTechnology.Core.Tale/Models/DataField.cs` — EDIT — `extension` blocks
- `tests/SolTechnology.Core.Tale.Tests/*.cs` — EDIT — remove `#region`

## Changes
- No public or protected signature change. Extension blocks keep the containing static class name (binary-compatible).
- No log-template change (that is step 16).
- Premortem RM-16 (core step 00, 2026-11-02, scenarios 16 and 23):
  - Moving `AddSolTale` into an `extension` block keeps `[MethodImpl(MethodImplOptions.NoInlining)]` and the `Assembly.GetCallingAssembly()` capture from step 05 on the method the consumer calls.
  - These guard tests keep their names and assertions; the only allowed diff in their files is `#region` removal:
    - `AddSolTale_NoArgs_ScansCallerAssembly` (step 05);
    - the step 10 history tests;
    - `Start_NonExposedHandler_Returns404`, `Start_ScannedButNotExposed_Returns404AndLogsWarning` (step 11);
    - the step 02 Tale-error round-trip tests;
    - `Handle_Direct_InteractiveChapterPauses_LogsNotPersistedWarning` (step 03).

## Acceptance criteria
- [x] `find src/SolTechnology.Core.Tale -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [x] `grep -rn "#region" tests/SolTechnology.Core.Tale.Tests` prints nothing.
- [x] `dotnet test tests/SolTechnology.Core.Tale.Tests` is green, and the DreamTravel component tests are green.
- [x] Public API diff is empty.
- [x] Premortem mitigations for `ModuleInstaller.cs` are applied.
- [x] RM-16: `grep -n "NoInlining" src/SolTechnology.Core.Tale/ModuleInstaller.cs` matches; `git diff` of the guard tests' files shows only `#region`/`#endregion` lines removed; the guard tests are green.

## Open questions
- none

## Deviations

### Partial files, two internal collaborators, and `TaleHandler`'s constructor

**Original plan:** Controller DTOs to `Api/TaleControllerModels.cs`; `TaleManager`, `TaleHandler`
and `Tale` under 150 lines with primary constructors; `extension` blocks in `ModuleInstaller`,
`TaleBuilderExtensions` and `DataField.cs`.

**Actual implementation:**
- Moving the DTOs left `TaleController` at about 325 lines, so its reflection calls into the generic
  `TaleManager` methods moved to the internal `TaleManagerInvoker`, and its response mapping to the
  partial file `TaleController.Responses.cs` (141 + 123 lines).
- `TaleManager` (primary constructor) keeps its public methods in `TaleManager.cs` (114) and the
  run pipeline in the partial `TaleManager.Pipeline.cs` (92); the resume checks and context
  restore moved to the internal `ResumableTaleLoader` (61).
- `TaleHandler` keeps its hand-written `protected` constructor and fields: a primary constructor
  would make it `public`, a public API change. Its internal `Run` moved to the partial
  `TaleHandler.Run.cs` (141 + 65 lines).
- `Tale<TContext,TOutput>` builder methods share a private `Add` step, which brings `Tale.cs` to
  138 lines; `TaleHandlerRegistry` moved from `ModuleInstaller.cs` to its own file.
- `AddSolTale` keeps `[MethodImpl(MethodImplOptions.NoInlining)]` and the
  `Assembly.GetCallingAssembly()` capture inside the `extension` block; the IL of the generated
  static `ModuleInstaller.AddSolTale(IServiceCollection, …)` is `noinlining` and calls
  `GetCallingAssembly` (RM-16, scenario 23).
- Public API: `Microsoft.DotNet.ApiCompat.Tool` 10.0.403 (default rules, both directions) found no
  breaking change against `ad0542d8`; strict mode lists only the two compiler-generated `extension`
  marker types, as in the CQRS feature.
- Tests: the eight files with `#region` lost only their `#region`/`#endregion` lines
  (`git diff -U0 tests/` shows nothing else); 130 Tale tests and the DreamTravel suites passed.

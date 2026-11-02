---
spec: 2026-11-02-logging-chronicle
step: 11
status: to-do
---

# Step 11: Tale auto-steps — one step per run and per chapter

## Summary
A Tale run becomes a step named after its handler, and every executed chapter becomes a child
step named after its `ChapterId`, so a Tale reads as a chapter list inside the report of its
request, command, or job. Tale gains a `ProjectReference` to Logging (CP-01, B3); it already has
the same `FrameworkReference`, so no external package is added. `IChronicle` is resolved from the
run's `IServiceProvider`, so no public Tale constructor changes. `AddSolTale` registers Chronicle
(CP-03).

## Affected components
- `src/SolTechnology.Core.Tale/SolTechnology.Core.Tale.csproj` — EDIT — `ProjectReference ..\SolTechnology.Core.Logging\SolTechnology.Core.Logging.csproj` (CP-01)
- `src/SolTechnology.Core.Tale/ModuleInstaller.cs` — EDIT — `AddSolTale` calls `services.AddSolChronicle()` (CP-03)
- `src/SolTechnology.Core.Tale/TaleHandler.cs` — EDIT — `Handle(TInput, JsonElement?, CancellationToken)` wraps the run in `Begin($"Tale {GetType().Name}")` with attribute `TaleId`; `IChronicle` from `_serviceProvider.GetService<IChronicle>()` (null → no steps)
- `src/SolTechnology.Core.Tale/Orchestration/TaleEngine.cs` — EDIT — passes the resolved `IChronicle?` to the interpreter once per run
- `src/SolTechnology.Core.Tale/Orchestration/TaleStepInterpreter.cs` — EDIT (created by `2026-11-02-core-2-0-tale` step "Guide conformance — Tale engine split") — chapter execution wrapped in `Step($"Chapter {chapter.ChapterId}")`
- `tests/SolTechnology.Core.Tale.Tests/SolTechnology.Core.Tale.Tests.csproj` — EDIT — `PackageReference Microsoft.Extensions.Diagnostics.Testing` (same version as step 04)
- `tests/SolTechnology.Core.Tale.Tests/TaleChronicleTests.cs` — NEW
- `tests/SolTechnology.Core.Tale.Tests/RegistrationDefaultsTests.cs` — EDIT

## Changes
- Chapter outcome mapping:
  - `Result` success → `Ok`.
  - Terminal failure → `Fail(error.GetType().Name, error.Message)`; the Tale step is `Failed` as well.
  - Interactive chapter waiting for input (including a re-pause after a validation error) → `Ok` with attribute `Outcome=WaitingForInput`.
  - Caller cancellation (the caller-only rule from `2026-11-02-core-2-0-tale` step "Tale engine — caller-only cancellation, no resume of failed tales, deterministic tests") → `Cancel()`.
  - Chapters skipped on resume because they are already `Completed` get no step.
- The step name uses `ChapterId`, not the history key from `2026-11-02-core-2-0-tale` step "Tale — chapter history keyed by step index and full type name", so names stay readable and aggregate by chapter.
- Interactive chapter input is never recorded as an attribute (PII).
- Logging must still have no `ProjectReference` (cycle guard).
- Tests:
  - `Handle_ThreeChapters_TaleStepHasThreeChapterChildrenInOrder`.
  - `Handle_ChapterFails_ChapterAndTaleStepsFailedAndLaterChaptersAbsent`.
  - `Handle_CallerCancelled_ChapterCancelled`.
  - `Handle_InteractiveChapterPauses_ChapterOkWithWaitingForInput`.
  - `Handle_Resume_CompletedChaptersHaveNoStep`.
  - `Handle_InsideRoot_TaleIsChildOfAmbientStep`.
  - `Handle_OutsideRoot_EmitsOne2150Entry`.
  - `Handle_ChronicleNotRegistered_RunsUnchanged`.
  - `AddSolTale_RegistersChronicle`.

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.Tale.Tests` is green.
- [ ] `grep -c "SolTechnology.Core.Logging.csproj" src/SolTechnology.Core.Tale/SolTechnology.Core.Tale.csproj` prints `1`.
- [ ] `grep -n "ProjectReference" src/SolTechnology.Core.Logging/SolTechnology.Core.Logging.csproj` prints nothing.
- [ ] `d=$(mktemp -d) && dotnet pack src/SolTechnology.Core.Tale -c Release -o "$d" && unzip -p "$d"/SolTechnology.Core.Tale.*.nupkg SolTechnology.Core.Tale.nuspec | grep 'id="SolTechnology.Core.Logging"'` prints one dependency line with the same version as Tale.
- [ ] `git diff -- src/SolTechnology.Core.Tale/SolTechnology.Core.Tale.csproj | grep "^+.*PackageReference"` prints nothing.
- [ ] `dotnet build SolTechnology.Core.slnx` is green, and `cd sample-tale-code-apps/DreamTravel && dotnet build` is green.
- [ ] Premortem mitigations for `ModuleInstaller.cs` (step 00) are applied.

## Open questions
- none

## Deviations

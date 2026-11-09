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
- `src/SolTechnology.Core.Tale/TaleHandler.Run.cs` — EDIT — the internal `Run(TContext, JsonElement?, TaleRunOptions, CancellationToken)`, which serves both the direct `Handle` and `TaleManager`'s start and resume, wraps the run in `Begin($"Tale {GetType().Name}")` with attribute `TaleId` (set after `_engine.Initialize`, which assigns it); `IChronicle` from `_serviceProvider.GetService<IChronicle>()` (null → no steps); the step status is set from the returned `Result<TOutput>` (decision R5)
- `src/SolTechnology.Core.Tale/Orchestration/TaleEngine.cs` — EDIT — resolves `IChronicle?` from its `serviceProvider` once per run and passes it to the `ChapterReader` it creates; stays ≤ 150 lines (133 at `c30d8998`)
- `src/SolTechnology.Core.Tale/Orchestration/ChapterReader.cs` — EDIT — `Read(...)`, where the chapter's outcome is known (result, caller cancellation, lease loss, exception), wraps the read in `Step($"Chapter {chapter.ChapterId}")`. `TaleEngine.ExecuteChapter` calls it for plain, fallback, and resumed chapters; `TaleStepInterpreter` only passes `ExecuteChapter` as a delegate and is not edited
- `tests/SolTechnology.Core.Tale.Tests/SolTechnology.Core.Tale.Tests.csproj` — EDIT — `PackageReference Microsoft.Extensions.Diagnostics.Testing` (version from `Directory.Packages.props`, added by step 04)
- `tests/SolTechnology.Core.Tale.Tests/TaleChronicleTests.cs` — NEW
- `tests/SolTechnology.Core.Tale.Tests/RegistrationDefaultsTests.cs` — EDIT
- `tests/SolTechnology.Core.Tale.Tests/TaleLeaseHeartbeatTests.cs` — read only — its takeover setup is the model for `Handle_LeaseTakenOver_ChapterCancelledAndTaleFailed`

## Changes
- Chapter outcome mapping (in `ChapterReader.Read`):
  - `Result` success → `Ok`.
  - Failed `Result` → `Fail(error.GetType().Name, error.Message)`.
  - Exception (the engine stores the generic "Chapter [X] failed unexpectedly.") → `Fail(exception)`.
  - Interactive chapter waiting for input (including a re-pause after a validation error) → `Ok` with attribute `Outcome=WaitingForInput`.
  - Caller cancellation (`OperationCanceledException` while the run's token is cancelled; the caller-only rule of the 2.0 Tale engine) → `Cancel()`.
  - Lease taken over by another run (`OperationCanceledException` while `TaleRunState.SaveError` is set: the heartbeat met `ConflictError` and cancelled the chapter token, `docs/architecture/tale-framework.md` "Persistence and concurrency") → `Cancel()` with attribute `Outcome=LeaseLost`.
  - Chapters skipped on resume because they are already `Completed` (`TaleRunState.Track` returns `null`), and chapters skipped because the tale already failed, was cancelled, or is paused, get no step. A chapter type missing from DI fails the tale without a chapter step.
- Tale step outcome mapping (in `TaleHandler.Run`, from the returned `Result<TOutput>`):
  - Success → `Ok`.
  - `TalePausedError` → `Ok` with attribute `Outcome=WaitingForInput`.
  - `TaleCancelledError` → `Cancel()`.
  - Any other failure, including `ConflictError` after a lease takeover → `Fail(error.GetType().Name, error.Message)`.
- Persisted runs: `TaleLeaseHeartbeat` renews the lease on a `TimeProvider` timer that is created
  inside the Tale step, so a SqlClient-backed repository's renewal commands appear as `SQL` steps
  under the Tale step, next to the chapters. This is expected and bounded by `MaxSteps`; the
  history-key and lease semantics do not change.
- The step name uses `ChapterId`, not the history key from `2026-11-02-core-2-0-tale` step "Tale — chapter history keyed by step index and full type name", so names stay readable and aggregate by chapter.
- Interactive chapter input is never recorded as an attribute (PII).
- Logging must still have no `ProjectReference` (cycle guard).
- Tests (`FakeLogCollector`; tree assertions on `FakeLogRecord.Message`):
  - `Handle_ThreeChapters_TaleStepHasThreeChapterChildrenInOrder`.
  - `Handle_ChapterFails_ChapterAndTaleStepsFailedAndLaterChaptersAbsent`.
  - `Handle_CallerCancelled_ChapterCancelled`.
  - `Handle_LeaseTakenOver_ChapterCancelledAndTaleFailed` (managed run through `TaleManager`).
  - `StartStory_ManagedRun_TaleStepHasChapterChildren`.
  - `Handle_InteractiveChapterPauses_ChapterOkWithWaitingForInput`.
  - `Handle_Resume_CompletedChaptersHaveNoStep`.
  - `Handle_InsideRoot_TaleIsChildOfAmbientStep`.
  - `Handle_OutsideRoot_EmitsOne2154Entry`.
  - `Handle_ChronicleNotRegistered_RunsUnchanged`.
  - `AddSolTale_RegistersChronicle`.

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.Tale.Tests` is green.
- [ ] `find src/SolTechnology.Core.Tale -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` lists no file this step edited.
- [ ] `grep -c "SolTechnology.Core.Logging.csproj" src/SolTechnology.Core.Tale/SolTechnology.Core.Tale.csproj` prints `1`.
- [ ] `grep -n "ProjectReference" src/SolTechnology.Core.Logging/SolTechnology.Core.Logging.csproj` prints nothing.
- [ ] `d=$(mktemp -d) && dotnet pack src/SolTechnology.Core.Tale -c Release -o "$d" && unzip -p "$d"/SolTechnology.Core.Tale.*.nupkg SolTechnology.Core.Tale.nuspec | grep 'id="SolTechnology.Core.Logging"'` prints one dependency line with the same version as Tale.
- [ ] `git diff -- src/SolTechnology.Core.Tale/SolTechnology.Core.Tale.csproj | grep "^+.*PackageReference"` prints nothing.
- [ ] `dotnet build SolTechnology.Core.slnx` is green, and `cd sample-tale-code-apps/DreamTravel && dotnet build` is green.
- [ ] Premortem mitigations for `ModuleInstaller.cs` (step 00) are applied.

## Open questions
- none

## Deviations

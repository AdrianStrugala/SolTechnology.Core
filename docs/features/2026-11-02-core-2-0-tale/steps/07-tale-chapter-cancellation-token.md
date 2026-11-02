---
spec: 2026-11-02-core-2-0-tale
step: 07
status: to-do
---

# Step 07: Tale — `CancellationToken` on chapters

## Summary
Chapters never receive a `CancellationToken`. Cancellation is only checked between chapters, so
HTTP or DB work inside a chapter cannot be cancelled (`cqrs-tale#9`). 2.0 adds the token to
`IChapter.Read` and `IInteractiveChapter.ReadWithInput`, and to their base classes (`C-03`,
`C-04`). Every chapter in the repository and the sample must be updated in the same PR.

Former core step 39; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.Tale/IChapter.cs` — EDIT — `:25`
- `src/SolTechnology.Core.Tale/Chapter.cs` — EDIT — `:37` and the XML example `:11-13`
- `src/SolTechnology.Core.Tale/IInteractiveChapter.cs` — EDIT — `:22`
- `src/SolTechnology.Core.Tale/InteractiveChapter.cs` — EDIT — `:28`
- `src/SolTechnology.Core.Tale/Orchestration/TaleEngine.cs` — EDIT — pass the run token; `InteractiveChapterReflection` (`:475`) invocation
- `tests/SolTechnology.Core.Tale.Tests/**` — EDIT — test chapters
- `sample-tale-code-apps/DreamTravel/src/LogicLayer/**` — EDIT — every `Read(`/`ReadWithInput(` override (`grep -rln "override Task<Result> Read" sample-tale-code-apps`)
- `.github/skills/command-query-event-tale/SKILL.md` — EDIT — chapter template signature

## Changes
- Signature: `Task<Result> Read(TContext context, CancellationToken cancellationToken)`. No default value; the engine always supplies it.
- Test: `Chapter_ReceivesRunToken_AndCancellationStopsInFlightWork`.

## Acceptance criteria
- [ ] `dotnet build SolTechnology.Core.slnx` is green, including DreamTravel.
- [ ] `dotnet test tests/SolTechnology.Core.Tale.Tests` is green.
- [ ] The DreamTravel unit and component tests are green.

## Open questions
- none (`C-03`, `C-04` confirmed by the user on 2026-11-02)

## Deviations

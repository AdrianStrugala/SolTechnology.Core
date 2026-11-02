---
spec: 2026-11-02-flagship-showcase-hygiene
step: 05
status: to-do
---

# Step 05: DreamTravel — logic and domain cleanup

## Summary
The sample has several smaller defects (`repo#B5`, `B10`, `B12`):

- `DreamTravel.Domain` references `SolTechnology.Core.CQRS` only to implement `IEvent`, which
  breaks the "Domain references nothing" rule (Guide §1/§6).
- `LimitCostOfPathsService` is a handler named `*Service`, and it mutates the caller's
  `query.Paths`.
- The showcased Tale has a duplicate `using`.
- The currency symbol is mojibake.
- The installer starts with 4 BOMs.

The event moves into the LogicLayer, so no public Core symbol moves (the `IEvent` move is "Not
planned").

## Affected components
- `sample-tale-code-apps/DreamTravel/src/DreamTravel.Domain/DreamTravel.Domain.csproj` — EDIT — `:4` remove the CQRS reference
- `sample-tale-code-apps/DreamTravel/src/DreamTravel.Domain/Events/CitySearched.cs` — DELETE
- `sample-tale-code-apps/DreamTravel/src/LogicLayer/DreamTravel.Queries/Events/CitySearched.cs` — NEW — same record, `: IEvent` (publishers: `FindCityByNameHandler.cs`, `FindCityByCoordinatesHandler.cs`)
- `sample-tale-code-apps/DreamTravel/src/Presentation/DreamTravel.Worker/EventHandlers/OnCitySearched/LogEventInfoJob.cs`, `SaveCitySearchJob.cs` — EDIT — `using DreamTravel.Queries.Events;`
- `sample-tale-code-apps/DreamTravel/src/Presentation/DreamTravel.Worker/DreamTravel.Worker.csproj` — EDIT — `ProjectReference` to `DreamTravel.Queries` only if not already present (Presentation → Logic is allowed by Guide §1)
- `sample-tale-code-apps/DreamTravel/src/LogicLayer/DreamTravel.Queries/LimitCostOfPaths/LimitCostOfPathsService.cs` — DELETE → `LimitCostOfPathsHandler.cs` — NEW — same logic on a copy of `query.Paths`
- `sample-tale-code-apps/DreamTravel/src/LogicLayer/DreamTravel.Queries/CalculateBestPath/CalculateBestPathTale.cs` — EDIT — `:5-6` duplicate using
- `sample-tale-code-apps/DreamTravel/src/LogicLayer/DreamTravel.Queries/ModuleInstaller.cs` — EDIT — `:1` BOMs
- `sample-tale-code-apps/DreamTravel/src/Presentation/DreamTravel.Api/Program.cs` — EDIT — `:50` currency symbol
- `sample-tale-code-apps/DreamTravel/tests/Unit/DreamTravel.Queries.UnitTests/**` — EDIT — renamed handler; test that the input list is not mutated

## Changes
- `git mv` for renames to keep history.
- Prerequisite: core step 39 is merged ([`2026-11-02-core-2-0-flagship-hardening`](../../2026-11-02-core-2-0-flagship-hardening.md)); it edits the same `DreamTravel.Queries` chapter files.
- Persisted-contract note (premortem trigger, found at the split on 2026-11-02): DreamTravel registers `AddSolPersistentEvents()` (`DreamTravel.Api/Program.cs:125`, `DreamTravel.Worker/Program.cs:55`), so `CitySearched` is stored as a Hangfire job argument. Moving it from `DreamTravel.Domain.Events` to `DreamTravel.Queries.Events` changes the stored type name; jobs enqueued before the move may fail to deserialize. The premortem (step 00) decides the mitigation.

## Acceptance criteria
- [ ] `grep -n "SolTechnology.Core.CQRS" sample-tale-code-apps/DreamTravel/src/DreamTravel.Domain/DreamTravel.Domain.csproj` prints nothing.
- [ ] `cd sample-tale-code-apps/DreamTravel && dotnet build` is green; unit and component tests green.
- [ ] Premortem mitigations for `ModuleInstaller.cs` are applied (`DreamTravel.Queries/ModuleInstaller.cs` BOM removal and handler rename).

## Open questions
- none

## Deviations

---
spec: 2026-11-02-flagship-showcase-hygiene
step: 05
status: done
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
- `sample-tale-code-apps/DreamTravel/src/Presentation/DreamTravel.Worker/EventHandlers/OnCitySearched/LogEventInfoJob.cs`, `SaveCitySearchJob.cs` — EDIT — `using DreamTravel.Queries.Events;` only; the `"DUPA."` log message at `LogEventInfoJob.cs:12` is a kept joke (premortem SRM-8)
- `sample-tale-code-apps/DreamTravel/src/Presentation/DreamTravel.Worker/DreamTravel.Worker.csproj` — EDIT — NEW `ProjectReference` to `DreamTravel.Queries` (verified 2026-11-10: not present; the Worker references Domain, Commands, and ServiceDefaults only. Presentation → Logic is allowed by Guide §1)
- `sample-tale-code-apps/DreamTravel/src/LogicLayer/DreamTravel.Queries/LimitCostOfPaths/LimitCostOfPathsService.cs` — DELETE → `LimitCostOfPathsHandler.cs` — NEW — same logic on a copy of `query.Paths` whose elements are new `Path` instances (premortem SRM-16)
- `sample-tale-code-apps/DreamTravel/src/LogicLayer/DreamTravel.Queries/CalculateBestPath/CalculateBestPathTale.cs` — EDIT — `:5-6` duplicate using
- `sample-tale-code-apps/DreamTravel/src/LogicLayer/DreamTravel.Queries/ModuleInstaller.cs` — EDIT — `:1` BOMs
- `sample-tale-code-apps/DreamTravel/src/Presentation/DreamTravel.Api/Program.cs` — EDIT — `:50` currency symbol (U+FFFD today) → `€` (decision 2026-11-10: the domain's cost unit, see the `[€]` comment in `CalculateBestPath/Chapters/2.FindProfitablePath.cs:56`); `:33` `"dupa"` stays (SRM-8)
- `sample-tale-code-apps/DreamTravel/tests/Unit/DreamTravel.Queries.UnitTests/**` — EDIT — renamed handler; test that neither the input list nor its elements are mutated (SRM-16)
- `docs/ClaudeCodingGuide.md` — EDIT — §0 item 4 (`:23`), §1 tree comment (`:53`), §6 events bullet (`:300`): records published through CQRS (`IEvent`) live in an `Events/` folder of the publishing LogicLayer project; the Domain keeps types that reference nothing (premortem SRM-15)
- `.github/skills/command-query-event-tale/SKILL.md` — EDIT — `:16`, `:57`, `:166`, `:235` the same rule and the new `CitySearched` path (premortem SRM-15)

## Changes
- `git mv` for renames to keep history.
- Prerequisite: tale 07 ([`2026-11-02-core-2-0-tale`](../../2026-11-02-core-2-0-tale.md); former core step 39) is merged; it edits the same `DreamTravel.Queries` chapter files. Done (verified 2026-11-10); the line references above still match `c0eb9482`.
- Removing the Domain's CQRS reference breaks no transitive consumer (verified 2026-11-10): `CitySearched.cs` is the only Domain file that uses `SolTechnology.Core.*`; `DreamTravel.Ui` and `DreamTravel.Commands` reference CQRS directly; the Worker gets it through Commands; GraphDatabase, Sql, and GeolocationDataClients use no CQRS type.
- Persisted-contract note (premortem trigger, found at the split on 2026-11-02): DreamTravel registers `AddSolPersistentEvents()` (`DreamTravel.Api/Program.cs:125`, `DreamTravel.Worker/Program.cs:56`), so `CitySearched` is stored as a Hangfire job argument. Moving it from `DreamTravel.Domain.Events` to `DreamTravel.Queries.Events` changes the stored type name; jobs enqueued before the move may fail to deserialize. The premortem (step 00) accepted this as SRM-14 (below).

- Premortem SRM-14 (step 00, 2026-11-10, scenario 14, accepted under H1): Hangfire jobs stored before this change carry `DreamTravel.Domain.Events.CitySearched, DreamTravel.Domain` and fail to load afterwards (`[AutomaticRetry(Attempts = 0)]` sends them straight to Failed). Only local databases hold such jobs; the Aspire SQL container is persistent, so they survive between runs. No type-name binder is added. Record the break under Deviations and in the brief, with the remedy: delete the Failed jobs in the Hangfire dashboard (`/hangfire/ui` on the Worker). `FindCityAndSaveDetailsTest` is the end-to-end control for the new type.
- Premortem SRM-15 (scenario 15): the Guide and the `command-query-event-tale` skill change in the same PR as the move, so no commit teaches `*.Domain/Events/` for `IEvent` records while the code does the opposite.
- Premortem SRM-16 (scenario 16): `Path` is a mutable class (`DreamTravel.Domain/Paths/Path.cs`); a list copy alone still writes into the caller's objects.
- Premortem SRM-8 (scenario 8): kept jokes are not edited — `"DUPA."` in `LogEventInfoJob.cs`, `CorsPolicy = "dupa"` in `Program.cs`.

## Acceptance criteria
- [x] `grep -n "SolTechnology.Core.CQRS" sample-tale-code-apps/DreamTravel/src/DreamTravel.Domain/DreamTravel.Domain.csproj` prints nothing.
- [x] `cd sample-tale-code-apps/DreamTravel && dotnet build` is green; unit and component tests green.
- [x] Premortem mitigations for `ModuleInstaller.cs` are applied (`DreamTravel.Queries/ModuleInstaller.cs` BOM removal and handler rename).
- [x] SRM-15: `grep -n "Domain/Events" .github/skills/command-query-event-tale/SKILL.md` prints nothing, and Guide §6 names the LogicLayer `Events/` folder for `IEvent` records.
- [x] SRM-16: the unit test asserts the input elements' `OptimalCost` and `OptimalDistance` after the call.
- [x] SRM-8: `grep -c "DUPA" .../LogEventInfoJob.cs` prints `1`; `grep -n 'CorsPolicy = "dupa"' .../DreamTravel.Api/Program.cs` prints one line.
- [x] SRM-14: the break and its remedy are recorded under Deviations.

## Open questions
- none

## Deviations

### Persisted `CitySearched` type name changes (SRM-14, accepted)

**Original plan:** H1 (a): move `CitySearched` to `DreamTravel.Queries.Events`; the premortem
accepted the persisted type-name break without a binder.

**Actual implementation:** as planned. Hangfire jobs enqueued before this commit store
`DreamTravel.Domain.Events.CitySearched, DreamTravel.Domain`; that type no longer exists, so the
Worker cannot load them and `[AutomaticRetry(Attempts = 0)]` moves them straight to Failed. Only
local developer databases hold such jobs (the Aspire SQL container is persistent, so they survive
between runs). Remedy: open the Hangfire dashboard on the Worker (`/hangfire/ui`), go to Jobs →
Failed, and delete the failed `CitySearched` jobs; or drop the local database volume. No
production data is affected. `FindCityAndSaveDetailsTest` passes with the new type end to end
(API publishes, Worker loads and saves).

### Test file renamed and tidied

**Original plan:** the unit tests follow the renamed handler and gain the SRM-16 test.

**Actual implementation:** `LimitCostOfPathsTests.cs` became `LimitCostOfPathsHandlerTests.cs`
(`git mv`, class renamed to match the production type), and its runs of two or three blank lines
were collapsed so the file stays under 150 lines (146) with the new test. The rewritten handler
also gets braces on its one braceless `if` (Guide §9.11).

### SRM outcomes (2026-11-10)

- **SRM-8 — done.** `LogEventInfoJob.cs` keeps `$"{nameof(CitySearched)}. DUPA."` (only its
  `using` changed; `grep -c DUPA` prints 1), and `CorsPolicy = "dupa"` is untouched; the
  `Program.cs` edit is the currency line only.
- **SRM-14 — accepted, recorded above** and in the brief.
- **SRM-15 — done in the same commit as the move.** Guide §0 item 4, the §1 tree (Domain line and
  Queries line), and §6 put `IEvent` records in the publishing LogicLayer project's `Events/`
  folder; the `command-query-event-tale` skill (`:16`, `:57`, `:166`, `:235`) says the same and
  points at `DreamTravel.Queries/Events/CitySearched.cs`.
- **SRM-16 — done.** The handler copies every `Path` (`Copy` creates new instances; the immutable
  `City` references are shared) before sorting and writing. The new unit test asserts the result
  values, the input list order, the input elements' `OptimalCost`/`OptimalDistance` (unchanged at
  7 and 8), and that no result element is an input instance. With only a list copy the test fails
  on the element values.

### Verification (2026-11-10)

- `dotnet build SolTechnology.Core.slnx -c Release`: 0 errors (97 warnings, all pre-existing;
  103 before step 03).
- DreamTravel Release tests: Queries unit 7 passed (6 + 1), TravelingSalesmanProblem 4, Ui 17,
  GeolocationDataClients 4 passed / 11 skipped, Component 40 passed.
- `ModuleInstaller.cs` starts with `using` (no BOM); the currency symbol is `€` (U+20AC).

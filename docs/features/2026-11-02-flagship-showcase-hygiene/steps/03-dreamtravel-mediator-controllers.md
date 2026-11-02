---
spec: 2026-11-02-flagship-showcase-hygiene
step: 03
status: to-do
---

# Step 03: DreamTravel — mediator-only, thin controllers

## Summary
Guide §7 holds up `CalculateBestPathController` as its `IMediator` template, but the real
controller breaks that template (`repo#B1`, `B2`, `B9`, `P1-1`):

- It injects `IQueryHandler<,>`, wraps the call in `try/catch`, passes `CancellationToken.None`,
  and has a Polish `<summary>`.
- On failure `Data` is null: v1 throws a `NullReferenceException` and returns 400, while
  `FindNameOfCity` returns 200 with a null body.
- `ProjectsController` injects DataLayer repositories directly, which skips validation and
  logging.

## Affected components
- `sample-tale-code-apps/DreamTravel/src/Presentation/DreamTravel.Api/Controllers/Trips/v2/CalculateBestPathController.cs` — EDIT — `:17,22,31-41,55` `IMediator`, `HttpContext.RequestAborted`, English summary, return the `Result`
- `.../Controllers/Trips/v1/FindNameOfCityController.cs` — EDIT — `:20,38-52`
- `.../Controllers/Trips/v1/LimitCostOfPathsController.cs` — EDIT — `:40-57`
- `.../Controllers/Trips/v1/FindLocationOfCityController.cs` — EDIT — `:38-47`
- `.../Controllers/RoadPlanner/ProjectsController.cs` — EDIT — `:10-12` through `IMediator`
- `sample-tale-code-apps/DreamTravel/src/LogicLayer/DreamTravel.Queries/GetIntersections/` — NEW — query + handler (+ validator if inputs)
- `sample-tale-code-apps/DreamTravel/src/LogicLayer/DreamTravel.Queries/GetStreets/` — NEW — query + handler
- `sample-tale-code-apps/DreamTravel/tests/Component/**` — EDIT — failure-path assertions (ProblemDetails, not 200/400 with exception text)

## Changes
- Prerequisite: core steps 08 (Production component test in the same test project) and 22 (`ResultConversionFilter` turns a failed `Result` into ProblemDetails) are merged ([`2026-11-02-core-2-0-flagship-hardening`](../../2026-11-02-core-2-0-flagship-hardening.md)).
- Use the [`command-query-event-tale`](../../../../.github/skills/command-query-event-tale/SKILL.md) skill for the two new queries.
- Locate exact v1 paths with `grep -rln "class FindNameOfCityController\|class LimitCostOfPathsController\|class FindLocationOfCityController\|class ProjectsController" sample-tale-code-apps` (the `...` prefix is `sample-tale-code-apps/DreamTravel/src/Presentation/DreamTravel.Api`).

## Acceptance criteria
- [ ] `grep -rnE "IQueryHandler<|ICommandHandler<|CancellationToken\.None|catch \(" sample-tale-code-apps/DreamTravel/src/Presentation/DreamTravel.Api/Controllers` prints nothing.
- [ ] `cd sample-tale-code-apps/DreamTravel && dotnet build` is green; component tests green, including a failing-query case returning ProblemDetails.

## Open questions
- none

## Deviations

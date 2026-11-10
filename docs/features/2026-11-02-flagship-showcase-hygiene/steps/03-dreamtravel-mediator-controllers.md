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
- `.../Controllers/Trips/v1/FindNameOfCityController.cs` — EDIT — `:20-29,38-52`
- `.../Controllers/Trips/v1/LimitCostOfPathsController.cs` — EDIT — `:19-28,40-62`
- `.../Controllers/Trips/v1/FindLocationOfCityController.cs` — EDIT — `:20-29,38-52`
- `.../Controllers/RoadPlanner/ProjectsController.cs` — EDIT — `:10-12` through `IMediator`
- `sample-tale-code-apps/DreamTravel/src/LogicLayer/DreamTravel.Queries/GetIntersections/` — NEW — query + handler (+ validator if inputs)
- `sample-tale-code-apps/DreamTravel/src/LogicLayer/DreamTravel.Queries/GetStreets/` — NEW — query + handler
- `sample-tale-code-apps/DreamTravel/src/LogicLayer/DreamTravel.Queries/DreamTravel.Queries.csproj` — EDIT — `ProjectReference` to `DreamTravel.GraphDatabase` (resync 2026-11-10: `IIntersectionRepository`/`IStreetRepository` live there and Queries does not reference it yet; LogicLayer → DataLayer is allowed by Guide §1, and `DreamTravel.Commands` already has the same reference)
- `sample-tale-code-apps/DreamTravel/tests/Component/**` — EDIT — failure-path assertions (ProblemDetails, not 200/400 with exception text)
- `sample-tale-code-apps/DreamTravel/tests/Component/ContractTests.ContractTest_reviewChangesToTheApi.verified.txt` — EDIT — the Swagger snapshot changes with the `[ProducesResponseType]` edits; review and accept the diff deliberately (resync 2026-11-10: not listed before)

## Changes
- Prerequisite: core 05 ([`2026-11-02-core-2-0-flagship-hardening`](../../2026-11-02-core-2-0-flagship-hardening.md); Production component test in the same test project) and api-auth 05 ([`2026-11-02-core-2-0-api-auth`](../../2026-11-02-core-2-0-api-auth.md); `ResultConversionFilter` turns a failed `Result` into ProblemDetails) are merged (former core steps 08 and 22). Done (verified 2026-11-10 at `c0eb9482`): `ProductionErrorHandlingTests.cs` exists, and `AddSolApiCoreFilters()` in `DreamTravel.Api/Program.cs` wires `ExceptionFilter` and `ResultConversionFilter`. The filter unwraps a successful `Result<T>` to `T` and turns a failed one into ProblemDetails, so returning the `Result` keeps the success body shape of every endpoint whose current body is already `result.Data`.
- Use the [`command-query-event-tale`](../../../../.github/skills/command-query-event-tale/SKILL.md) skill for the two new queries.
- The `...` prefix is `sample-tale-code-apps/DreamTravel/src/Presentation/DreamTravel.Api` (paths verified 2026-11-10).
- Chronicle (completed 2026-11-10) shipped before this step, although its plan expected this step first: keep `FindCityChronicleTests` green and do not change `AddSolChronicle` or the `LogDetail` wiring beyond what this step names.

## Acceptance criteria
- [ ] `grep -rnE "IQueryHandler<|ICommandHandler<|CancellationToken\.None|catch \(" sample-tale-code-apps/DreamTravel/src/Presentation/DreamTravel.Api/Controllers` prints nothing.
- [ ] `cd sample-tale-code-apps/DreamTravel && dotnet build` is green; component tests green, including a failing-query case returning ProblemDetails.

## Open questions
- none

## Deviations

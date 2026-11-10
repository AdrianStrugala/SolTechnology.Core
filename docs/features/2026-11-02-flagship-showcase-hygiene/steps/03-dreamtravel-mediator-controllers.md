---
spec: 2026-11-02-flagship-showcase-hygiene
step: 03
status: done
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
- `.../Controllers/RoadPlanner/ProjectsController.cs` — EDIT — `:10-12` through `IMediator`; `:17,25` declare the unwrapped list type, not `ActionResult<Result<…>>` (premortem SRM-9)
- `.../Controllers/Trips/v2/FindCityByNameController.cs`, `FindCityByCoordinatesController.cs`, `StatisticsController.cs`, `.../Controllers/RoadPlanner/TrafficController.cs` — EDIT — pass `HttpContext.RequestAborted` to `Send` (premortem SRM-7)
- `sample-tale-code-apps/DreamTravel/src/Presentation/DreamTravel.Api/Program.cs` — EDIT — `:121` `LogDetail` endpoint `/api/v1/FindLocationOfCity` → `/api/FindLocationOfCity` (premortem SRM-19); nothing else in this file
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

- Premortem SRM-5 (step 00, 2026-11-10, scenario 5: returning the `Result` from `CalculateBestPathV1` changes the v1 body from `List<Path>` to `CalculateBestPathResult`, because `ResultConversionFilter` unwraps it): v1 projects a successful `Result` to `BestPaths` before returning it, so the success body stays a JSON array; a component test with `X-API-VERSION: 1.0` pins the array on success and ProblemDetails on failure.
- Premortem SRM-6 (scenario 6, decision recorded under the owner's pre-approval): `LimitCostOfPathsController` reads `HttpContext.Session`, but `Program.cs` configures no session, so the endpoint always failed (400 with the exception text). Without the `try/catch` it would become a 500 with a Critical log on every call. It now takes `[FromBody] LimitCostOfPathsQuery` and sends it through `IMediator`; the Session read and the double JSON encoding of the body go. No working client can break. Component tests: success and validation failure.
- Premortem SRM-7 (scenario 7, sweep `cqrs.md`): every controller action passes `HttpContext.RequestAborted`, including the four v2 actions that already use `IMediator`.
- Premortem SRM-8 (scenario 8): kept verbatim — the "Fire!" log lines of actions that keep logging (`TSP Engine V1 (deprecated): Fire!`, `TSP Engine V2: Fire!`, `Limit Cost Engine: Fire!`) are jokes (Guide §9.10); the concatenated v1 templates (`"Looking for city: " + …`) are Guide §15 "report only", so they stay and are listed under Deviations. The `"dupa"` CORS name in `Program.cs` stays untouched by the `LogDetail` edit.
- Premortem SRM-9 (scenario 9): `[ProducesResponseType]` declares the unwrapped payload type for 200 and `ProblemDetails` for failures; the reviewed Swagger snapshot shows no `Result` envelope for the edited actions.
- Premortem SRM-19 (scenario 19, sweep `logging.md`): the v1 `LogDetail` path matches the header-versioned route, so v1 city searches log `CityName` like v2.

## Acceptance criteria
- [x] `grep -rnE "IQueryHandler<|ICommandHandler<|CancellationToken\.None|catch \(" sample-tale-code-apps/DreamTravel/src/Presentation/DreamTravel.Api/Controllers` prints nothing.
- [x] `cd sample-tale-code-apps/DreamTravel && dotnet build` is green; component tests green, including a failing-query case returning ProblemDetails.
- [x] SRM-5: a v1 best-path component test asserts a JSON array body on success.
- [x] SRM-6: `grep -n "Session" .../Controllers/Trips/v1/LimitCostOfPathsController.cs` prints nothing; its component tests pass.
- [x] SRM-7: `grep -rn "mediator.Send(" sample-tale-code-apps/DreamTravel/src/Presentation/DreamTravel.Api/Controllers | grep -v RequestAborted` prints nothing.
- [x] SRM-8: `grep -n 'CorsPolicy = "dupa"' sample-tale-code-apps/DreamTravel/src/Presentation/DreamTravel.Api/Program.cs` prints one line; `grep -rn "Fire!" .../Controllers` still prints the kept log lines.
- [x] SRM-9: `grep -rn "typeof(Result<" .../Controllers` and `grep -rn "ActionResult<Result<" .../Controllers` print nothing.

## Open questions
- none

## Deviations

### LimitCost validator

**Original plan:** SRM-6 adds component tests for success and validation failure.

**Actual implementation:** `LimitCostOfPathsQuery` had no validator, so a validation failure was
impossible. `LimitCostOfPathsQueryValidator` (same file) requires `CostLimit >= 0` and a non-empty
`Paths`. The controller's logger also moved from `ILogger<Controller>` to
`ILogger<LimitCostOfPathsController>` (Guide §11), which changes the category of the kept
"Limit Cost Engine: Fire!" line.

### v1 best-path failure case

**Original plan:** SRM-5's v1 component test pins ProblemDetails on failure.

**Actual implementation:** the failure case posts `{"cities": null}` and gets a 400
ValidationProblemDetails. A single-city request, the obvious query failure, returns 200 with `[]`
in both versions: `CalculateBestPathTale` loses the tale in `Expect(NoOfCities > 1)`, and the later
`Otherwise<JustOrderCities>()` recovers that loss. That is a Tale-flow defect outside this step;
it is recorded as a follow-up for step 08.

### LogDetail test sends a buffered body

**Original plan:** SRM-19 fixes the path.

**Actual implementation:** the path is fixed, and `FindLocationOfCityV1Tests` proves it (the test
fails with the old `/api/v1/...` path). The test posts a `StringContent` body: the
`SolTechnology.Core.HTTP` request builder sends `JsonContent` without `Content-Length`, and the
logging middleware buffers only bodies with a length, so `LogDetail(... Body ...)` never fires for
requests built with `CreateRequest(...).WithBody(...)`. That library interplay is a follow-up for
step 08, not fixed here.

### Report-only log templates and other kept items (SRM-8)

**Original plan:** report-only templates stay and are listed here.

**Actual implementation:** kept verbatim: `"Looking for city: " + query.Name`
(v1 `FindLocationOfCityController`), `"Looking for city: " + query.Lat + ";" + query.Lng`
(v1 `FindNameOfCityController` and v2 `FindCityByCoordinatesController`). Also kept:
`TrafficController` declares `ActionResult<RecalculateTrafficCommand>` although the command returns
`List<Street>`, and its handler is commented out, so the endpoint always fails with "no handler";
only the token changed (SRM-7). Reported for step 08.

### SRM outcomes (2026-11-10)

- **SRM-5 — done.** v1 projects a successful `Result` to `BestPaths` through a target-typed
  `Result<List<Path>>`, so the success body stays a JSON array.
  `CalculateBestPathV1Tests` (header `X-API-VERSION: 1.0`) asserts a JSON array of 3 paths on
  success and a 400 ProblemDetails object on failure.
- **SRM-6 — done.** `LimitCost` binds `[FromBody] LimitCostOfPathsQuery` and sends it through
  `IMediator`; no `Session`, no `JsonConvert`. `LimitCostOfPathsTests` cover success (element
  values after the cost limit) and validation failure (errors for `CostLimit` and `Paths`).
- **SRM-7 — done.** Every `mediator.Send` in `Controllers/` passes `HttpContext.RequestAborted`,
  including FindCityByName, FindCityByCoordinates, Statistics, Traffic, and Projects.
- **SRM-8 — done.** "dupa", the three "Fire!" lines, and the report-only templates above are
  unchanged; the `Program.cs` edit is the one `LogDetail` line.
- **SRM-9 — done.** `ProjectsController` declares `List<Intersection>` / `List<Street>` and a
  ProblemDetails 500; every v1 action declares ProblemDetails for 400. The snapshot no longer has
  the `IntersectionListResult`, `StreetListResult`, or `Error` schemas.
- **SRM-19 — done.** `LogDetail` lists `/api/FindLocationOfCity`; the v1 test asserts `CityName`
  in the controller log's scope.

### Contract snapshot (Swagger) before and after

The v2 document (the verified snapshot, which also lists the v1-only controllers) and the v1
document were dumped before and after the change with a temporary, uncommitted test. Every
difference was reviewed and is intended:

- `FindLocationOfCity`, `FindNameOfCity`: 400 schema `ValidationResult[]` → `ProblemDetails`.
- v1 `CalculateBestPath` (v1 document only): 400 schema `string` → `ProblemDetails`.
- `LimitCost`: query parameters `costLimit` and `sessionId` → JSON body `LimitCostOfPathsQuery`
  (`costLimit` minimum 0, `paths` required with at least one item); 400 `string` → `ProblemDetails`.
- `projects/nodes`, `projects/streets`: 200 `IntersectionListResult` / `StreetListResult` → arrays
  of `Intersection` / `Street`; new 500 `ProblemDetails`.
- Removed schemas: `IntersectionListResult`, `StreetListResult`, `Error`, `ValidationResult`;
  added: `LimitCostOfPathsQuery`.

The v1 contract changes are listed for readers in the DreamTravel README (API section). DreamTravel
has no changelog of its own, and the root `CHANGELOG.md` covers the published packages only.

### Verification (2026-11-10)

- `dotnet build SolTechnology.Core.slnx` and the component test project build with 0 errors.
- DreamTravel component tests: 40 passed (34 before + 6 new). Unit tests: GeolocationDataClients
  4 passed / 11 skipped, Queries 6, TravelingSalesmanProblem 4, Ui 17 — all green.
- All acceptance greps print what they should.

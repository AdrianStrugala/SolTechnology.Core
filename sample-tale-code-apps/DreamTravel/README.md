# Dream Travel

*"Observing the past centuries we can conclude, that the humanity tend to search for optimal roads. Merchants, travelers, and explorers were creating more and more detailed maps. In the digital era, everyone can use free and open real-time maps found in the Internet. Some of them provide a feature for finding the shortest road connecting a set of cities, but very few can answer common question “Is it really worth to pay for toll highway or should I just stay on the free road?” A solution of Traveling Eco-Salesman Problem provides an answer to this question.
<br>
 Program is written in C#, implements Ant Colony Optimization and God alorithms."*


 That's the original Readme of Dream Travel app, written in ~2016 as part of my master thesis. Feeling old now.

## Run in 2 minutes

### Prerequisites

- .NET 10 SDK (10.0.100 or later).
- Docker Desktop, running. On Apple silicon turn on "Use Rosetta for x86_64/amd64 emulation" in
  Docker Desktop's settings: the SQL Server image is amd64 only.
- Free local ports 1404 (SQL Server), 7474 and 7687 (Neo4j), 2020/2021 (API), 2025 (Worker),
  7024 (UI), and 17060 (Aspire dashboard).
- A trusted HTTPS development certificate, once per machine: `dotnet dev-certs https --trust`.
- Internet access on the first start (container images, NuGet packages, the Neo4j APOC plugin).

There is nothing to configure. The local-only development credentials (the SQL Server password,
the Neo4j password, and the API key `<SECRET>`) are committed on purpose; they protect nothing but
containers on your machine.

### Start

```bash
git clone https://github.com/AdrianStrugala/SolTechnology.Core.git
cd SolTechnology.Core/sample-tale-code-apps/DreamTravel
dotnet build src/Infrastructure/DreamTravelDatabase/DreamTravelDatabase.csproj
dotnet run --project src/DreamTravel.Aspire
```

The `dotnet build` line writes `artifacts/dacpac/DreamTravelDatabase.dacpac`, which Aspire deploys
into SQL Server once the container is up. Skip it and the deployment fails, so the API and the
Worker wait forever. `dotnet run` builds the API, the Worker, and the UI itself.

The first start pulls the SQL Server and Neo4j images, which takes a few minutes (longer under
amd64 emulation). The containers are persistent, so later starts reuse them. The console prints
the dashboard link with its login token:
`Login to the dashboard at https://localhost:17060/login?t=...`.

- On macOS, Aspire reads the development certificate's private key from the keychain when it
  starts. If a keychain prompt appears, allow it; the start waits until you answer. If the start
  fails with `User canceled the operation` (no prompt could be shown, for example over SSH), set
  `ASPIRE_DCP_USE_DEVELOPER_CERTIFICATE=false` and
  `ASPIRE_DEVELOPER_CERTIFICATE_DEFAULT_HTTPS_TERMINATION=false` in that shell and start again.
- Neo4j loads the Wrocław street graph (`Data/wroclaw_drive.graphml`) when it is empty. If the
  console prints `Failed to initialize Neo4j graph`, Neo4j was not accepting connections yet: stop
  with Ctrl+C and start again, and the next start imports the graph.

| What | Where |
|---|---|
| Aspire dashboard (resources, logs, traces) | the login link printed to the console |
| API and Swagger UI | `https://localhost:2021/swagger` (HTTP on 2020 redirects to HTTPS) |
| Worker: Hangfire dashboard, GraphQL playground | `http://localhost:2025/hangfire/ui`, `http://localhost:2025/graphql/ui` |
| UI (see "Known limitations") | `https://localhost:7024` |
| Neo4j browser | `http://localhost:7474` (user `neo4j`, password `neo4jpass`) |

### Smoke test

These two calls need no Google key and return 200:

```bash
curl -k https://localhost:2021/healthz
curl -k -H "X-API-KEY: <SECRET>" -H "X-API-VERSION: 2.0" https://localhost:2021/api/statistics/countries
```

`<SECRET>` is the literal local API key, and `X-API-VERSION` selects the API version (the routes
carry no version). `-k` skips curl's certificate check for the localhost development certificate.

### Calling Google-backed endpoints (optional, your own key)

`FindCityByName`, `FindCityByCoordinates`, and `CalculateBestPath` call Google. The committed
configuration points Google at `http://localhost:2137/google/`, which only the component tests'
WireMock server answers, so these endpoints fail locally until you bring your own Google Maps
Platform key (Geocoding and Distance Matrix APIs). Set it in the shell that starts Aspire, and
never commit it:

```bash
export HTTPClients__Google__BaseAddress=https://maps.googleapis.com/
export HTTPClients__Google__Options__Key=<your Google key>
dotnet run --project src/DreamTravel.Aspire
```

### Known limitations

- **The UI is not aligned with the API yet.** Aspire starts `DreamTravel.Ui`, but the UI still
  expects `Result` envelopes, calls `/api/v2/...` routes while the API versions by header, uses
  `https://localhost:2021` as a fixed base URL, and ships `<SECRET>` as its Google Maps key, so its
  map pages need your own Google Maps key and do not work end to end today. Use Swagger or `curl`
  instead; the alignment is planned work.
- Toll costs come from the Michelin API, which has no key configured locally.

### Running the tests

From this folder (Docker must be running for the component tests):

```bash
dotnet test tests/Component
dotnet test tests/Unit/DreamTravel.Queries.UnitTests
```

The folder has no solution file of its own; the DreamTravel projects are part of the repository's
`SolTechnology.Core.slnx`.

## Overview

DreamTravel is the reference application for the SolTechnology.Core libraries and the Tale Code
approach described in the [Coding Guide](../../docs/ClaudeCodingGuide.md). It solves the Traveling
Salesman Problem with a toll-or-free-road cost trade-off, and it keeps a Neo4j street graph of
Wrocław for traffic analysis.

### Technology

- **.NET 10**, ASP.NET Core API, Blazor WebAssembly UI with MudBlazor.
- **.NET Aspire** orchestrates the local environment (`src/DreamTravel.Aspire`).
- **SQL Server** with EF Core for cities and search statistics; it also stores the Hangfire jobs.
- **Hangfire** runs background jobs and persistent CQRS events in the Worker.
- **Neo4j** holds the street graph.

The application uses no Azure Service Bus, no Blob Storage, and no Quartz.NET.

### Projects

Built from the actual project references (SolTechnology.Core libraries in the last column).

| Project | Layer | References | SolTechnology.Core |
|---|---|---|---|
| `DreamTravel.Api` | Presentation | Queries, Commands, Workflows, GraphDatabase, ServiceDefaults | API, Auth, CQRS |
| `DreamTravel.Worker` | Presentation | Domain, Commands, Queries, ServiceDefaults | — (through Commands) |
| `DreamTravel.Ui` | Presentation | Domain | HTTP, CQRS |
| `DreamTravel.ServiceDefaults` | Presentation | — | — |
| `DreamTravel.Commands` | Logic | DomainServices, TravelingSalesmanProblem, GraphDatabase, Infrastructure, Domain | CQRS |
| `DreamTravel.Queries` | Logic | DomainServices, TravelingSalesmanProblem, GraphDatabase | CQRS, Tale |
| `DreamTravel.DomainServices` | Logic | GeolocationDataClients, Sql | SQL, Tale |
| `DreamTravel.Workflows` | Logic | — | CQRS, Tale |
| `DreamTravel.TravelingSalesmanProblem` | Logic | — | — |
| `DreamTravel.Sql` | Data | Domain | AUID, Hangfire, SQL |
| `DreamTravel.GraphDatabase` | Data | Domain | — |
| `DreamTravel.GeolocationDataClients` | Data | Domain | HTTP, Cache |
| `DreamTravel.SQLite` | Data (used by tests) | — | Tale |
| `DreamTravel.Infrastructure` | Infrastructure | — | Hangfire |
| `DreamTravelDatabase` | Infrastructure | — (SQL project, builds the dacpac) | — |
| `DreamTravel.Domain` | Domain | nothing | — |
| `DreamTravel.Aspire` | Host | Api, Worker, Ui | SQL |

### Application Components

#### 1. **DreamTravel.Ui** - Blazor WebAssembly Frontend
Located: `src/Presentation/DreamTravel.Ui`

**Framework:** Blazor WASM + MudBlazor UI components. See "Known limitations" above: the UI does not
work against the current API yet.

**Pages:**
- **Home** (`/`) - Application overview with feature cards
- **Trip Planner** (`/tsp-map`) - TSP route optimization on a Google map (city search, multi-city
  route calculation, toll vs free road costs)
- **City Architect** (`/map`) - Street network visualization and editing on the Neo4j graph
- **Flow** (`/flow`) - Workflow management (placeholder)

#### 2. **DreamTravel.Api** - REST API Backend
Located: `src/Presentation/DreamTravel.Api`

**Endpoints** (version chosen with the `X-API-VERSION` header; every call needs `X-API-KEY`):
```
POST /api/FindCityByName           v2  - Find a city by name (Google geocoding, cached in SQL)
POST /api/FindCityByCoordinates    v2  - Reverse geocoding
POST /api/CalculateBestPath        v2  - TSP solver with the toll/free cost trade-off (v1: paths only)
GET  /api/statistics/countries     v2  - Search statistics per country
POST /api/FindLocationOfCity       v1  - Deprecated
POST /api/FindNameOfCity           v1  - Deprecated
POST /api/LimitCost                v1  - Deprecated
GET  /api/projects/nodes|streets       - Street graph from Neo4j
POST /api/Traffic/recalculate          - Traffic recalculation (no handler registered yet)
     /api/dreamtravel/tale/...         - Start, resume, and read interactive Tales
GET  /healthz                          - JSON health report
```

**Features:**
- Thin controllers that send every request through the CQRS `IMediator`
- FluentValidation pipeline
- `Result` pattern; `ResultConversionFilter` turns failures into ProblemDetails
- API key authentication, header-based API versioning, Swagger/OpenAPI
- Chronicle step reports (below)

**Contract changes of the deprecated v1 endpoints (November 2026):** every controller now sends its
request through `IMediator`, so v1 gets the same validation and error format as v2.

- Failures of `FindLocationOfCity`, `FindNameOfCity`, `LimitCost`, and v1 `CalculateBestPath` are
  RFC 7807 ProblemDetails (400 for validation, 404 for not found, 500 otherwise) instead of a 400
  with a JSON-encoded exception message. `FindLocationOfCity` and `FindNameOfCity` no longer answer
  a failed lookup with an empty success response.
- v1 `CalculateBestPath` still returns a plain JSON array of paths on success.
- `LimitCost` takes `{ "costLimit": 20, "paths": [ ... ] }` as its JSON body instead of the
  `costLimit` and `sessionId` query parameters (it read the paths from a session that was never
  configured, so it always failed), and it returns the paths as a JSON array instead of a
  JSON-encoded string. A negative limit or an empty path list is a 400.
- A v1 `FindLocationOfCity` request now adds `CityName` to its log scope, like v2 `FindCityByName`.
- `GET api/projects/nodes` and `GET api/projects/streets` keep their JSON arrays; Swagger now
  declares the arrays instead of a `Result` envelope.

**External Integrations:**
- Google Maps API (geocoding, distance matrix)
- Michelin API (toll costs)
- GeoDB API (city search)

#### 3. **DreamTravel.Worker** - Background Processing
Located: `src/Presentation/DreamTravel.Worker`

**Responsibilities:**
- Hangfire server: runs the persistent `CitySearched` event handlers (`OnCitySearched/`), which save
  searched cities and their statistics
- The `FetchTrafficJob` recurring job (registered with `Cron.Never()`, so it runs only when
  triggered from the Hangfire dashboard)
- Hangfire dashboard (`/hangfire/ui`) and a GraphQL endpoint over the SQL database (`/graphql`,
  playground at `/graphql/ui`, Voyager at `/voyager/ui`)

**Technologies:** ASP.NET Core host, Hangfire with SQL Server storage, EntityGraphQL

#### 4. **DreamTravel.Aspire** - Orchestration
Located: `src/DreamTravel.Aspire`

**.NET Aspire** starts and wires:
- SQL Server (persistent container on port 1404) and the `DreamTravelDatabase` dacpac deployment
- Neo4j 5.10 with APOC (persistent container on ports 7474/7687), loading
  `Data/wroclaw_drive.graphml` on the first start
- The API, the Worker, and the UI, plus the dashboard with logs, traces, and health

### Testing Strategy

#### **Unit Tests** (`tests/Unit/`)
- **DreamTravel.Ui.UnitTests** - Blazor component tests (bUnit)
- **DreamTravel.Queries.UnitTests** - Tale chapters and query handlers
- **DreamTravel.TravelingSalesmanProblem.UnitTests** - TSP algorithms
- **DreamTravel.GeolocationDataClients.IntegrationTests** - HTTP policy configuration; the tests
  against the paid Google and Michelin APIs are `[Ignore]`d
- **DreamTravel.Commands.UnitTests** - empty for now

**Framework:** NUnit + FluentAssertions

#### **Component Tests** (`tests/Component/`)
- **DreamTravel.Component.Tests** - the API and the Worker in memory against real infrastructure
  - `APIFixture` (WebApplicationFactory) for the API and the Worker
  - `SQLFixture` (Testcontainers SQL Server, dacpac deployed)
  - `WireMockFixture` with a fake Google API
  - A Swagger contract snapshot (Verify)

**Test Pattern** (from `CalculateBestPathFeatureTest`):
```csharp
_wireMockFixture.Fake<IGoogleHTTPClient>()
    .WithRequest(x => x.GetDurationMatrixByFreeRoad(cities))
    .WithResponse(x => x.WithSuccess().WithBody(GoogleFakeApi.FreeDistanceMatrix));

var apiResponse = await _apiClient
    .CreateRequest("/api/CalculateBestPath")
    .WithHeader("X-API-KEY", "<SECRET>")
    .WithHeader("X-API-VERSION", "2.0")
    .WithBody(new { Cities = cities })
    .Post<CalculateBestPathResult>();

apiResponse.BestPaths[0].StartingCity.Name.Should().Be("Wroclaw");
```

#### **End-to-End Tests** (`tests/EndToEnd/`)
- **DreamTravel.E2E.Tests** - a placeholder (`HelloWorldTest`); no browser tests yet

### Data Layer Architecture

#### **SQL Server** (`DreamTravel.Sql`, schema in `DreamTravelDatabase`)
- Cities, their alternative names, and search statistics per city and country
- Hangfire job storage

**ORM:** Entity Framework Core

#### **Neo4j Graph Database** (`DreamTravel.GraphDatabase`)
- Street network topology of Wrocław: intersections and streets

**Client:** Neo4j.Driver

### Chronicle step reports

The API and the Worker bind `Logging:Core:Chronicle` with `AddSolChronicle(builder.Configuration)`,
so every request, Hangfire job and fire-and-forget event writes one
[Chronicle](../../docs/Log.md#chronicle) report. Development turns the flight recorder on and the
console's `SingleLine` off, so the tree stays readable. This is the report of one
`POST /api/FindCityByName` from the component test `FindCityChronicleTests`, for a city missing from
the database:

```text
Finished request [POST] [/api/FindCityByName] -> [200] in [1201 ms]. Chronicle: [Ok] [
HTTP POST [@0 ms] [1201 ms] [ok] Route=api/FindCityByName StatusCode=200
|- SQL DECLARE [@40 ms] [13 ms] [ok]
|- SQL SELECT [@53 ms] [1 ms] [ok]
`- FindCityByNameQuery [@76 ms] [1105 ms] [ok] Name=Chroniclecb4cf5c0
   |- FetchCity [@156 ms] [966 ms] [ok]
   |  |- SQL SELECT [@652 ms] [8 ms] [ok]
   |  `- HTTP GET Google [@694 ms] [422 ms] [ok] Host=localhost StatusCode=200 Attempts=1
   |- FetchCityCached [@1123 ms] [3 ms] [ok]
   |  `- SQL SELECT [@1125 ms] [1 ms] [ok]
   |- SQL DECLARE [@1137 ms] [8 ms] [ok]
   |- SQL SELECT [@1149 ms] [5 ms] [ok]
   `- SQL INSERT [@1161 ms] [13 ms] [ok]]
```

`FetchCity` and `FetchCityCached` are the handler's own `IChronicle.Step` calls; the query step,
the Google call and the SQL commands (EF Core and the Hangfire job that persists `CitySearched`) are
recorded without user code. The event itself runs in the Worker, under its own `Event CitySearched` root.

### Design Patterns

One request, top to bottom. The complete example is
[CalculateBestPath](src/LogicLayer/DreamTravel.Queries/CalculateBestPath/).

1) Controller - thin: it logs, sends the query through `IMediator`, and returns the `Result`
([CalculateBestPathController](src/Presentation/DreamTravel.Api/Controllers/Trips/v2/CalculateBestPathController.cs)).
`ResultConversionFilter` sends a successful `Result<T>` as `T` and a failed one as ProblemDetails.

```csharp
[HttpPost]
[MapToApiVersion("2.0")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(CalculateBestPathResult), (int)HttpStatusCode.OK)]
[ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
public async Task<IActionResult> CalculateBestPathV2([FromBody] CalculateBestPathQuery query)
{
    logger.LogInformation("TSP Engine V2: Fire!");
    return Ok(await mediator.Send(query, HttpContext.RequestAborted));
}
```

2) Query - the input and its **validation**, in one file. The CQRS pipeline runs the validator
before the handler and returns a `ValidationError` when it fails.

```csharp
public class CalculateBestPathQuery : IQuery<CalculateBestPathResult>
{
    public List<CityQueryModel> Cities { get; set; } = new();
}

public class CalculateBestPathQueryValidator : AbstractValidator<CalculateBestPathQuery>
{
    public CalculateBestPathQueryValidator()
    {
        RuleFor(x => x.Cities)
            .NotNull();
    }
}
```

3) Tale - the handler is a Tale whose `Tell()` reads like a table of contents. Each chapter is one
class in `Chapters/`, and all state shared between chapters lives on `CalculateBestPathContext`.

```csharp
protected override Tale<CalculateBestPathResult> Tell() =>
    Open<InitiateContext>()
        .Expect(ctx => ctx.NoOfCities > 1,
                new NotFoundError { Message = "A route needs at least two cities." })
        .Read<DownloadRoadData>()
        .Read<FindProfitablePath>()
        .Otherwise<JustOrderCities>()
        .Read<SolveTsp>()
        .WhenLost(error => logger.LogWarning("Best path calculation failed: {Error}", error.Message))
        .Read<FormCalculateBestPathResult>()
        .Finale(ctx => ctx.Output);
```

4) Chapters - `DownloadRoadData` fetches the Google distance matrices and the Michelin costs in
parallel, `FindProfitablePath` weighs toll against free roads, `SolveTsp` orders the cities with
the ant colony engine, and `FormCalculateBestPathResult` builds the output.

5) Result - a plain DTO:

```csharp
public class CalculateBestPathResult
{
    public List<Path> BestPaths { get; set; } = new();
    public List<City> Cities { get; set; } = new();
}
```

### Using the NuGet packages

The sample references the libraries as projects (`ProjectReference`), so a change in `src/` is
visible immediately. An application of your own uses the published packages instead; the
equivalent of each reference is a `PackageReference` with the same name:

```xml
<!-- in the sample -->
<ProjectReference Include="..\..\..\..\..\src\SolTechnology.Core.CQRS\SolTechnology.Core.CQRS.csproj" />

<!-- in your application -->
<PackageReference Include="SolTechnology.Core.CQRS" Version="2.0.0" />
```

| DreamTravel project | Packages |
|---|---|
| `DreamTravel.Api` | `SolTechnology.Core.API`, `SolTechnology.Core.Auth`, `SolTechnology.Core.CQRS` |
| `DreamTravel.Ui` | `SolTechnology.Core.HTTP`, `SolTechnology.Core.CQRS` |
| `DreamTravel.Commands` | `SolTechnology.Core.CQRS` |
| `DreamTravel.Queries`, `DreamTravel.Workflows` | `SolTechnology.Core.CQRS`, `SolTechnology.Core.Tale` |
| `DreamTravel.DomainServices` | `SolTechnology.Core.SQL`, `SolTechnology.Core.Tale` |
| `DreamTravel.Sql` | `SolTechnology.Core.AUID`, `SolTechnology.Core.Hangfire`, `SolTechnology.Core.SQL` |
| `DreamTravel.GeolocationDataClients` | `SolTechnology.Core.HTTP`, `SolTechnology.Core.Cache` |
| `DreamTravel.Infrastructure` | `SolTechnology.Core.Hangfire` |
| `DreamTravel.SQLite` | `SolTechnology.Core.Tale` |
| `DreamTravel.Aspire` | `SolTechnology.Core.SQL` |
| `DreamTravel.Component.Tests` | `SolTechnology.Core.Testing`, `SolTechnology.Core.API.Testing`, `SolTechnology.Core.SQL.Testing`, `SolTechnology.Core.HTTP.Testing` |

Use the version this repository builds (`<Version>` in
[`src/Directory.Build.props`](../../src/Directory.Build.props)) once it is on nuget.org. With
Central Package Management, put the version in `Directory.Packages.props` as a `PackageVersion`
and leave it off the `PackageReference`. The packages bring their own dependencies, such as
`SolTechnology.Core` and `SolTechnology.Core.Logging`.

## Future Work

### UI Enhancements

#### Trip Planner (TSP)
- [ ] **AIIA Payment Integration** - Implement `/api/users/pay` endpoint for payment processing
  - Create `UserController` in DreamTravel.Api
  - Integrate with AIIA API for payment authorization
  - Add payment flow to UI (currently disabled)
  - Estimated effort: 3-4 days

- [ ] **Date Range Statistics** - Add DateRangePicker for filtering city search statistics
  - Allow users to view statistics for specific date ranges
  - Display trends over time
  - Export statistics to CSV/Excel

- [ ] **Path Details Modal** - Display detailed information for each route segment
  - Show distance, duration, cost breakdown
  - Display alternative routes
  - Include street-level navigation instructions

- [ ] **Export Route** - Export calculated routes in standard formats
  - GPX format for GPS devices
  - KML format for Google Earth
  - PDF route summary with maps

- [ ] **Multi-language Support** - Internationalize the UI
  - English, Polish, German translations
  - Currency conversion based on locale
  - Distance units (km/miles) preference

#### City Architect (Road Planner)
- [ ] **Traffic Prediction** - Machine learning model for traffic prediction
  - Historical data analysis
  - Time-of-day patterns
  - Special events impact

- [ ] **Street Quality Indicators** - Visual representation of road conditions
  - Pavement quality
  - Safety ratings
  - Accident statistics

- [ ] **Collaborative Editing** - Multi-user street network editing
  - Real-time updates via SignalR
  - Change tracking and approval workflow
  - Conflict resolution

### Testing & Quality

- [ ] **Unit Test Coverage** - Expand unit test coverage
  - TspService business logic
  - CitiesPanel component behavior
  - GoogleMap component interactions
  - Target: 80%+ code coverage

### Architecture & Performance

- [ ] **Caching Strategy** - Reduce API calls with intelligent caching
  - Redis for distance matrix results
  - Browser localStorage for user preferences
  - Cache invalidation strategies

- [ ] **API Rate Limiting** - Implement rate limiting for external APIs
  - Google Maps API quota management
  - Retry logic with exponential backoff
  - Fallback strategies

- [ ] **Background Processing** - Move heavy computations to background jobs
  - Progress notifications via SignalR

### DevOps & Monitoring

- [ ] **Application Insights** - Comprehensive telemetry and monitoring
  - User behavior analytics
  - Performance metrics
  - Error tracking and alerts

- [ ] **CD Pipeline** - Automated deployment pipeline
  - Staging environment
  - Blue-green deployments
  - Automated rollback on failures

- [ ] **Infrastructure as Code** - Terraform/Bicep for Azure resources
  - Reproducible environments
  - Environment parity (dev/staging/prod)
  - Cost optimization

### Documentation

- [ ] **API Documentation** - OpenAPI/Swagger enhancements
  - Request/response examples
  - Authentication guide
  - Rate limiting documentation

- [ ] **User Guide** - End-user documentation
  - Feature walkthroughs
  - Troubleshooting guide

---
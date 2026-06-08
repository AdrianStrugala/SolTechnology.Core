# Comprehensive Testing Guidelines

## Testing Pyramid Overview

```
          /\
         /  \
        /    \
       / PROD \
      /________\
     /   E2E    \
    /____________\
   /   COMPONENT  \
  /________________\
 /       UNIT       \
/____________________\
```

## Testing Levels Comparison

| Level | Execution | Environment | Volume            | Data | Scope | Frequency  | Use Cases |
|-------|---------------------|--------|-------------------|-------|-----------|------------|-----------|
| Production | Manual | Real Production | A Few             | Real data | Whole product | Rarely | Deployment validation, Performance monitoring, A/B testing, Chaos testing, User analytics |
| End to End | Automated in CD | Test & Sandbox | A few per feature | Defined scenarios | Whole product | After each merge | Critical user paths,  Authentication flows, Browser compatibility, Multi-step validations |
| Component | Automated in Build & CI | Local with mocked external dependencies | Many              | Mocked data | Single application | On each build | Service integration, Database operations, API contracts, Message handling, Configuration |
| Unit | Automated in Build & CI | No environment needed | Millions          | Mocked data | Single class/method | On each build | Business logic, Algorithms, Data transformations, Validation rules, Utility functions |

## 1. Production Testing

### Overview
Production testing involves verifying that the application works correctly in the actual production environment with real user data and traffic. This includes continuous monitoring, and manual testing campaigns.

### Key Principles
- **Real-World Validation**: Test with actual production data and infrastructure
- **Minimal Impact**: Tests should not affect real users or data
- **Continuous Monitoring**: Implement health checks and synthetic monitoring
- **Quick Feedback**: Rapid detection of production issues

### Manual testing campaign


### Monitoring
//TODO

## 2. End-to-End Testing

### Overview
End-to-End tests verify complete user workflows through the entire application stack. They test the system as a whole, including all integrated components, external services, and user interfaces, running in an environment that closely resembles production.

### Key Principles
- **Full Stack Testing**: Include UI, APIs, databases, and external services
- **User Journey Focus**: Test complete user scenarios from start to finish
- **Stability**: E2E tests work as deployment verification gate, they need to provide repeatable and reliable results
- **Critical Path Coverage**: Focus on the most important user workflows

### Tools
The key prerequisite is deep understanding of application flows and functionalities.
- **Testing framework**: [NUnit](https://github.com/nunit/nunit)
- **Frontend interactions**: [Playwright](https://github.com/microsoft/playwright)
- **Pipeline linking**: [example](https://github.com/AdrianStrugala/SolTechnology.Core/blob/bca831703c7fd251775ef95f9c568dc31025c6da/sample-tale-code-apps/DreamTravel/devOps/pipelines/build%26test.yml#L83)

### Example Repositories
- Dream Travel E2E Tests: https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/sample-tale-code-apps/DreamTravel/tests/EndToEnd

## 3. Component Testing

### Overview
Component tests (could be called also Integration or Functional) are designed to test the entire feature in an isolated environment. Verify that different parts of your application work together as expected. They test the integration between multiple units while still mocking external dependencies like APIs. These tests cover integration with APIs, external resources, storages, and specifically SQL databases.

### Key Principles
- **Single Functionality Scope**: Test interaction from the API to the data source and back
- **Controlled Environment**: Use test doubles for external dependencies
- **Business Logic Focus**: Verify complete business scenarios and SQL operations
- **Golden Mean**: Provides appropriate level of confidence while being reasonably costly in creation and maintenance
- **SQL Logic Coverage**: SQL queries and commands often contain specific logic and mapping that should be properly tested

### Setup

#### Provisioning containers — the `.Testing` companion packages

Component-test infrastructure ships as **NUnit `.Testing` companion packages**, referenced from test
projects only. DreamTravel's component suite composes three: `SolTechnology.Core.SQL.Testing`,
`SolTechnology.Core.HTTP.Testing` and `SolTechnology.Core.API.Testing` (see the
[Testing framework packages](#testing-framework-packages) map).

`SQLFixture` (from `SolTechnology.Core.SQL.Testing`) starts a SQL Server container, deploys the dacpac
and resets state between tests:

```csharp
SqlFixture = new SQLFixture("DreamTravelDatabase")
    .WithSQLProject("../../../../../src/Infrastructure/DreamTravelDatabase/DreamTravelDatabase.csproj");
await SqlFixture.InitializeAsync();
```

Prerequisites: **docker** and a **schema source**. DreamTravel uses a dacpac (`.sqlproj`); EF migrations
(`.WithEfMigrations(...)`), raw `.sql` scripts (`.WithScripts(...)`) and Postgres (`.UsePostgres()`) are
also supported — see [SQL.md](SQL.md#testing).

#### Initialize the Environment

A single assembly-level `[SetUpFixture]` boots every container **once** for the whole run.
`TestConfigurationBuilder` (from `SolTechnology.Core.API.Testing`) merges `appsettings.tests.json` with
the container connection string and the dynamic WireMock URL. The API host swaps the Hangfire publisher
for a deterministic in-process one via the service-override hook:

```csharp
[SetUpFixture]
[SetCulture("en-US")]
public static class ComponentTestsFixture
{
    public static APIFixture<Program> ApiFixture { get; set; } = null!;
    public static APIFixture<Worker.Program> WorkerFixture { get; set; } = null!;
    public static SQLFixture SqlFixture { get; set; } = null!;
    public static WireMockFixture WireMockFixture { get; set; } = null!;

    [OneTimeSetUp]
    public static async Task SetUp()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "development");

        SqlFixture = new SQLFixture("DreamTravelDatabase")
            .WithSQLProject(Path.GetFullPath(
                "../../../../../src/Infrastructure/DreamTravelDatabase/DreamTravelDatabase.csproj"));
        await SqlFixture.InitializeAsync();

        WireMockFixture = new WireMockFixture();
        WireMockFixture.Initialize();                 // dynamic port — read Url below
        WireMockFixture.RegisterFakeApi(new GoogleFakeApi());

        var configuration = new TestConfigurationBuilder()
            .AddJsonFile("appsettings.tests.json")
            .Override("Sql:ConnectionString", SqlFixture.DatabaseConnectionString)
            .Override("HTTPClients:Google:BaseAddress", $"{WireMockFixture.Url}/google/")
            .Build();

        WorkerFixture = new APIFixture<Worker.Program>(configuration);
        SyncHangfireNotificationPublisher.UseScopeFactory(
            () => WorkerFixture.TestServer.Services.GetRequiredService<IServiceScopeFactory>());

        ApiFixture = new APIFixture<Program>(configuration, services =>
        {
            services.RemoveAll<IHangfireNotificationPublisher>();
            services.AddSingleton<IHangfireNotificationPublisher, SyncHangfireNotificationPublisher>();
        });
    }

    [OneTimeTearDown]
    public static async Task TearDown()
    {
        await SqlFixture.DisposeAsync();               // no-op when TESTCONTAINERS_REUSE=true
        ApiFixture.Dispose();
        WorkerFixture.Dispose();
        WireMockFixture.Dispose();
    }
}
```

Reset SQL state between tests with `await SqlFixture.ResetAsync();` (Respawn-based, schema preserved) in a
`[TearDown]`; mocks are cleared with `WireMockFixture.Reset();`.

#### Define Test Scenarios

Use WireMockFixture to set up fake APIs that simulate the behavior of external services. This ensures that the tests run in an isolated environment without relying on actual external services.

Write test methods that cover various scenarios. For example, the FindsOptimalPath test verifies that the CalculateBestPath feature correctly finds the optimal path between a list of cities by interacting with the fake Google APIs.

```csharp
[Test]
public async Task FindsOptimalPath()
{
    // Given a list of cities
    var cities = new List<City>
    {
        new() { Name = "Wroclaw", Latitude =  51.107883, Longitude = 17.038538},
        new() { Name = "Firenze", Latitude =  43.769562, Longitude = 11.255814},
        new() { Name = "Vienna", Latitude = 48.210033, Longitude =  16.363449},
        new() { Name = "Barcelona",  Latitude =  41.390205, Longitude = 2.154007}
    };

    // Given a fake Google city API
    foreach (var city in cities)
    {
        _wireMockFixture.Fake<IGoogleHTTPClient>()
            .WithRequest(x => x.GetLocationOfCity(city.Name))
            .WithResponse(x => x.WithSuccess().WithBody(
            $@"{{
                   ""results"" :
                   [
                      {{
                         ""geometry"" :
                         {{
                            ""location"" :
                            {{
                               ""lat"" : {city.Latitude},
                               ""lng"" : {city.Longitude}
                            }}
                         }}
                      }}
                   ],
                   ""status"" : ""OK""
                }}"));
    }

    // Given a fake Google distance API
    _wireMockFixture.Fake<IGoogleHTTPClient>()
        .WithRequest(x => x.GetDurationMatrixByFreeRoad(cities))
        .WithResponse(x => x.WithSuccess().WithBody(GoogleFakeApi.FreeDistanceMatrix));

    _wireMockFixture.Fake<IGoogleHTTPClient>()
        .WithRequest(x => x.GetDurationMatrixByTollRoad(cities))
        .WithResponse(x => x.WithSuccess().WithBody(GoogleFakeApi.TollDistanceMatrix));

    // When user searches for the location of each city
    foreach (var city in cities)
    {
        var findCityByNameResponse = await _apiClient
            .CreateRequest("/api/v2/FindCityByName")
            .WithHeader("X-API-KEY", "<SECRET>")
            .WithBody(new { city.Name })
            .PostAsync<Result<City>>();

        findCityByNameResponse.IsSuccess.Should().BeTrue();
        findCityByNameResponse.Data.Should().BeEquivalentTo(city);
    }

    // And when user searches for the best path
    var apiResponse = await _apiClient
        .CreateRequest("/api/v2/CalculateBestPath")
        .WithHeader("X-API-KEY", "<SECRET>")
        .WithBody(new { Cities = cities })
        .PostAsync<Result<CalculateBestPathResult>>();

    // Assert expected results
    apiResponse.IsSuccess.Should().BeTrue();
    var paths = apiResponse.Data.BestPaths;

    // Then the returned path is optimal
    paths[0].StartingCity.Name.Should().Be("Wroclaw");
    paths[0].EndingCity.Name.Should().Be("Vienna");

    paths[1].StartingCity.Name.Should().Be("Vienna");
    paths[1].EndingCity.Name.Should().Be("Firenze");

    paths[2].StartingCity.Name.Should().Be("Firenze");
    paths[2].EndingCity.Name.Should().Be("Barcelona");
}
```

### Tools
- **Spawn required resources just for the test run**: [Testcontainers](https://dotnet.testcontainers.org/), wrapped by the `.Testing` companion packages
- **Spawn application under test**: `WebApplicationFactory`, wrapped by `APIFixture<T>` (`SolTechnology.Core.API.Testing`)
- **Mock external APIs**: WireMock.Net, wrapped by `WireMockFixture` + the `Fake<T>` DSL (`SolTechnology.Core.HTTP.Testing`)
- **Preferred testing framework**: NUnit

### Testing framework packages

Component-test infrastructure is modular: one `.Testing` NuGet companion per concern, all built on the
shared `SolTechnology.Core.Testing` foundation. Reference only what a suite needs — they compose in a
single `[SetUpFixture]`. Defined in [ADR-008](adr/008-testing-framework-companions.md).

| Concern | Package | Key types | Readme |
|---|---|---|---|
| Foundation: data attributes, `Retry`, container lifetime, log assertions | `SolTechnology.Core.Testing` | `AutoNSubstituteData`, `Retry`, `TestContainersContext`, `ContainerLifecycleHelper` | [Testing.md](Testing.md) |
| In-memory API host + config / auth helpers | `SolTechnology.Core.API.Testing` | `APIFixture<T>`, `TestConfigurationBuilder`, `CreateAuthorizedClient` | [Api.md#testing](Api.md#testing) |
| SQL (MSSQL + Postgres), dacpac / EF / scripts | `SolTechnology.Core.SQL.Testing` | `SQLFixture`, `ResetAsync`, `UsePostgres` | [SQL.md#testing](SQL.md#testing) |
| HTTP mocks (typed-client fakes) | `SolTechnology.Core.HTTP.Testing` | `WireMockFixture`, `Fake<T>`, `FakeApiBase` | [HTTP.Testing.md](HTTP.Testing.md) |
| Redis | `SolTechnology.Core.Redis.Testing` | `RedisFixture`, `FlushAsync` | [Redis.Testing.md](Redis.Testing.md) |
| Azure Blob (Azurite) | `SolTechnology.Core.BlobStorage.Testing` | `AzuriteFixture`, `ClearAsync` | [BlobStorage.Testing.md](BlobStorage.Testing.md) |
| Azure Service Bus (emulator) | `SolTechnology.Core.ServiceBus.Testing` | `ServiceBusFixture` | [ServiceBus.Testing.md](ServiceBus.Testing.md) |

> The data engine is **AutoFixture** (with NSubstitute auto-faking), not Bogus. Bogus ships as an
> *optional, complementary* realistic-value builder (`BogusCustomization`), not a replacement — see
> [Testing.md](Testing.md) and ADR-008.

### Container lifetime & reuse

Every container-backed fixture follows one lifetime model, centralised in `SolTechnology.Core.Testing`:

- **Within a run** — the assembly-level `[SetUpFixture]` `[OneTimeSetUp]` boots each container **once**;
  every test class shares them for free.
- **Across runs** — set `TESTCONTAINERS_REUSE=true` (env var or `.runsettings`) to keep containers alive
  between runs (Testcontainers-native `.WithReuse(true)` + stable names). `DisposeAsync()` becomes a
  no-op; `ContainerLifecycleHelper.EnsureRunningAsync` restarts a reused container stopped externally.
  Default is **off** so CI stays hermetic.
- **Between tests** — reset state without restarting: `SqlFixture.ResetAsync()` (Respawn),
  `RedisFixture.FlushAsync()`, `AzuriteFixture.ClearAsync()`, `WireMockFixture.Reset()`.
- **Readiness probes, not TCP-accept** — host-side login probe (SQL) and AMQP SASL-echo probe
  (Service Bus); Ryuk is disabled for Docker Desktop Enhanced Container Isolation.

### Example Repositories
- Dream Travel Component Tests: https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/sample-tale-code-apps/DreamTravel/tests/Component

## 4. Unit Testing

### Overview
Unit tests form the foundation of the testing pyramid. They are the most numerous, fastest, and least expensive tests to write. Unit tests verify the smallest testable parts of an application in isolation from the rest of the codebase.

### Key Principles
- **Isolation**: Test a single unit of work (method, class) in complete isolation
- **Fast Execution**: Should run in milliseconds
- **Deterministic**: Same input always produces the same output
- **Independent**: Tests should not depend on each other
- **Readable**: AAA (Arrange, Act, Assert) pattern, one assertion per test (when possible)

### Tools & Frameworks
//TODO

## Additional Resources

### Books
- "The Art of Unit Testing" by Roy Osherove
- "Growing Object-Oriented Software, Guided by Tests" by Steve Freeman and Nat Pryce
- "xUnit Test Patterns" by Gerard Meszaros

### Documentation
- Microsoft Testing Documentation
- Martin Fowler's Testing Articles

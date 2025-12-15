# Dream Travel

*"Observing the past centuries we can conclude, that the humanity tend to search for optimal roads. Merchants, travelers, and explorers were creating more and more detailed maps. In the digital era, everyone can use free and open real-time maps found in the Internet. Some of them provide a feature for finding the shortest road connecting a set of cities, but very few can answer common question “Is it really worth to pay for toll highway or should I just stay on the free road?” A solution of Traveling Eco-Salesman Problem provides an answer to this question.
<br>
 Program is written in C#, implements Ant Colony Optimization and God alorithms."*


 That's the original Readme of Dream Travel app, written in ~2016 as part of my master thesis. Feeling old now.

## Overview

DreamTravel is a comprehensive full-stack application demonstrating modern .NET architecture, CQRS patterns, and cloud-native design. The application solves the Traveling Salesman Problem (TSP) while providing real-time traffic analysis and route optimization.

### Current Architecture (2025)

**Technology Stack:**
- **.NET 10.0** - Latest .NET platform
- **Blazor WebAssembly** - Modern SPA framework
- **ASP.NET Core** - REST API
- **.NET Aspire** - Cloud-native orchestration
- **Azure Services** - Service Bus, SQL Database, Blob Storage
- **Neo4j** - Graph database for street networks

### Application Components

#### 1. **DreamTravel.Ui** - Blazor WebAssembly Frontend
Located: `backend/src/Presentation/DreamTravel.Ui`

**Framework:** Blazor WASM + MudBlazor UI components

**Pages:**
- **Home** (`/`) - Application overview with feature cards
- **Trip Planner** (`/tsp-map`) - TSP route optimization
  - Interactive Google Maps integration
  - City search (by name or coordinates)
  - Multi-city route calculation
  - Cost analysis (toll vs free roads)
  - Real-time distance matrix from Google Maps API
- **City Architect** (`/map`) - Street network visualization
  - Traffic speed heat maps
  - Interactive street editing
  - Neo4j graph database integration
  - Custom traffic analytics
- **Flow** (`/flow`) - Workflow management (placeholder)

**Key Features:**
- Responsive design (mobile + desktop)
- Google Maps JavaScript interop
- Real-time city search statistics
- Keyboard shortcuts (Ctrl+Enter for route calculation)
- Clean Architecture with shared components

#### 2. **DreamTravel.Api** - REST API Backend
Located: `backend/src/Presentation/DreamTravel.Api`

**Endpoints:**
```
POST /api/v2/FindCityByName          - Search cities by name (GeoDB API)
POST /api/v2/FindCityByCoordinates   - Reverse geocoding
POST /api/v2/CalculateBestPath       - TSP solver with cost optimization
GET  /api/v2/statistics/countries    - Search statistics aggregation
```

**Features:**
- CQRS pattern (Commands + Queries)
- FluentValidation pipeline
- Result pattern for error handling
- API key authentication
- Swagger/OpenAPI documentation

**External Integrations:**
- Google Maps API (geocoding, distance matrix)
- Michelin API (cost calculations)
- GeoDB API (city search)

#### 3. **DreamTravel.Worker** - Background Processing
Located: `backend/src/Presentation/DreamTravel.Worker`

**Responsibilities:**
- Azure Service Bus message processing
- Scheduled tasks (cron-based)
- Heavy TSP calculations (offloaded from API)
- Data synchronization jobs

**Technologies:**
- .NET Generic Host
- Azure Service Bus
- Quartz.NET scheduler

#### 4. **DreamTravel.Aspire** - Orchestration & Service Discovery
Located: `backend/src/DreamTravel.Aspire`

**.NET Aspire** provides:
- Service orchestration (API, Worker, UI)
- Service discovery
- Configuration management
- Health checks dashboard
- Distributed tracing
- Local development environment

**Services:**
- SQL Server (Azure SQL / local Docker)
- Neo4j (graph database)
- Azure Service Bus
- Blob Storage

### Testing Strategy

Comprehensive test coverage across multiple levels:

#### **Unit Tests** (`tests/Unit/`)
- **DreamTravel.Ui.UnitTests** - Blazor component tests (bUnit)
  - 17 tests for CitiesPanel, TspMap components
  - FluentAssertions for readable assertions
- **DreamTravel.Trips.Commands.UnitTests** - Business logic
- **DreamTravel.Trips.Queries.UnitTests** - Query handlers
- **DreamTravel.Trips.TravelingSalesmanProblem.UnitTests** - TSP algorithms

**Framework:** NUnit + FluentAssertions

#### **Component Tests** (`tests/Component/`)
- **DreamTravel.Component.Tests** - Integration tests with infrastructure
  - WebApplicationFactory (in-memory API)
  - Testcontainers (SQL Server)
  - WireMock (external API mocking)
  - BlazorWasmFixture (automated UI startup)

**Test Pattern:**
```csharp
[Test]
public async Task CalculateBestPath_WithMultipleCities_ReturnsOptimalRoute()
{
    // Arrange - Mock Google API
    _wireMock.Fake<IGoogleApiClient>()
        .WithRequest(x => x.GetDurationMatrixByFreeRoad, cities)
        .WithResponse(x => x.WithSuccess().WithBody(fakeMatrix));

    // Act - Call API
    var result = await _apiClient.PostAsync<Result<CalculateBestPathResult>>(
        "/api/v2/CalculateBestPath",
        new { Cities = cities }
    );

    // Assert - Verify optimal path
    result.IsSuccess.Should().BeTrue();
    result.Data.BestPaths.Should().HaveCount(3);
}
```

#### **End-to-End Tests** (`tests/EndToEnd/`)
- **DreamTravel.E2E.Tests** - Playwright browser automation
  - Full user workflows
  - Cross-browser testing
  - Deployment verification

**Framework:** Playwright + NUnit

### Data Layer Architecture

#### **SQL Database** (Azure SQL / SQL Server)
- User accounts and authentication
- Search statistics
- Application configuration

**ORM:** Entity Framework Core + Dapper

#### **Neo4j Graph Database**
- Street network topology
- Intersection nodes
- Traffic analytics
- Shortest path algorithms

**Client:** Neo4j.Driver

#### **Azure Blob Storage**
- File uploads
- Route export files (GPX, KML)

### Design Patterns

For detailed architecture patterns and code examples, see the sections below.

 Projects dependency diagram:


 ```mermaid
classDiagram
    DreamTravel <|-- Api : Presentation
    Api <|-- Trips : Logic 
    Api <|-- Identity : Logic

    Trips <|-- TripsCommands  : CQRS 
    Trips <|-- TripsQueries : in

    TripsQueries <|-- TripsHttpClients : Data 

    Identity <|-- IdentityCommands : action

    IdentityCommands <|-- IdentityDatabaseClient : Data 

    IdentityDatabaseClient <|-- Infrastructure : The base 
    TripsHttpClients <|-- Infrastructure : The base 
 

    class DreamTravel{
       The complete backend 
       app design
    }

    class Api{
      The .NET host for the app
      Exposes APIs
    }

    class Trips{
      Logic layer
      Module responsible for
      the path finding
    }

    class Identity{
      Logic layer 
      Module responsible for
      user management
    }

    class Infrastructure{
      The very technical, drivers part
    }

      class TripsHttpClients{
      Data Layer
      Provides clients for HTTP calls
    }

     class IdentityDatabaseClient{
      Data Layer
      Provides SQL queries and repositories
    }
```


Going into more details. The complete example of a [QueryHandler](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/sample-tale-code-apps/DreamTravel/backend/src/Trips/LogicLayer/DreamTravel.Trips.Queries/CalculateBestPath):

1) Controller - it is meant be simple:

```csharp
    [HttpPost]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(List<Path>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> CalculateBestPath([FromBody] CalculateBestPathQuery calculateBestPathQuery)
    {
        _logger.LogInformation("TSP Engine: Fire!");
        return await Return(_calculateBestPath.Handle(calculateBestPathQuery));
    }
```


2) Query Handler - is build of a few components:

a) Query - model and **validation**

```csharp
    public class CalculateBestPathQuery
    {
        public List<City?> Cities { get; set; } = new();
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

Validation is build into the CQRS pipeline, and whenever Query/Command validator is registered, it would be invoked and the result handled.

b) Context - intermediate model for internal handler operations. This one might be controvesial, but greatly increses performance of the solution. What is worth to remember, that this model lives only in the scope of Query.

```csharp
    public sealed class CalculateBestPathContext
    {
        public double[] FreeDistances { get; set; }
        public double[] TollDistances { get; set; }
        public double[] OptimalDistances { get; set; }
        public double[] Goals { get; set; }
        public double[] Costs { get; set; }
        public double[] OptimalCosts { get; set; }
        public double[] VinietaCosts { get; set; }


        public CalculateBestPathContext(int noOfCities)
        {
            int matrixSize = noOfCities * noOfCities;

            FreeDistances = new double[matrixSize];
            TollDistances = new double[matrixSize];
            OptimalDistances = new double[matrixSize];
            Goals = new double[matrixSize];
            Costs = new double[matrixSize];
            OptimalCosts = new double[matrixSize];
            VinietaCosts = new double[matrixSize];
        }
    }
```

c) Handler - the logic part. Contains the operations itself or orchestrates executors:

```csharp
    public async Task<CalculateBestPathResult> Handle(CalculateBestPathQuery query)
    {
        var cities = query.Cities.Where(c => c != null).ToList();
        var context = new CalculateBestPathContext(cities.Count);

        await _downloadRoadData(cities!, context);
        _findProfitablePath(context, cities.Count);

        var orderOfCities = _solveTSP(context.OptimalDistances.ToList());

        CalculateBestPathResult calculateBestPathResult = new CalculateBestPathResult
        {
            Cities = cities!,
            BestPaths = _formPathsFromMatrices(cities!, context, orderOfCities)
        };
        return calculateBestPathResult;
    }
```

d) Executors - the dirty part of the code has to be written somewhere. Those classes are modyfing context for the best performance:

```csharp
    public DownloadRoadData(IGoogleApiClient googleApiClient, IMichelinApiClient michelinApiClient)
    {
        _googleApiClient = googleApiClient;
        _michelinApiClient = michelinApiClient;
    }

    public async Task Execute(List<City> listOfCities, CalculateBestPathContext calculateBestPathContext)
    {
        List<Task> tasks = new List<Task>
        {
            Task.Run(async () => calculateBestPathContext.TollDistances = await _googleApiClient.GetDurationMatrixByTollRoad(listOfCities)),
            Task.Run(async () => calculateBestPathContext.FreeDistances = await _googleApiClient.GetDurationMatrixByFreeRoad(listOfCities))
        };

        tasks.AddRange(DownloadCostMatrix(listOfCities, calculateBestPathContext));

        await Task.WhenAll(tasks);
    }

    private List<Task> DownloadCostMatrix(List<City> listOfCities, CalculateBestPathContext calculateBestPathContext)
    {
        List<Task> tasks = new List<Task>();

        for (int i = 0; i < listOfCities.Count; i++)
        {
            for (int j = 0; j < listOfCities.Count; j++)
            {
                int iterator = j + i * listOfCities.Count;

                var i1 = i;
                var j1 = j;
                tasks.Add(Task.Run(async () => (calculateBestPathContext.Costs[iterator], calculateBestPathContext.VinietaCosts[iterator]) =
                    await _michelinApiClient.DownloadCostBetweenTwoCities(listOfCities[i1], listOfCities[j1])));
            }
        }

        return tasks;
    }
```

e) And finally the Result:

```csharp
public class CalculateBestPathResult
{
    public List<Path> BestPaths { get; set; }
    public List<City> Cities { get; set; }

    public CalculateBestPathResult()
    {
        BestPaths = new List<Path>();
        Cities = new List<City>();
    }
}
```

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

- [ ] **Component Integration Tests** - Playwright tests for UI workflows
  - `TspUiIntegrationTest.cs` - End-to-end TSP flow testing
  - Mock Google API responses with WireMock
  - Automated UI interaction testing
  - CI/CD integration

- [ ] **E2E Tests** - Full system end-to-end tests
  - Critical user journeys
  - Cross-browser compatibility (Chrome, Firefox, Safari)
  - Mobile responsiveness testing
  - Performance benchmarks

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
  - Azure Functions for TSP calculations
  - Queue-based processing (Azure Service Bus)
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
  - Getting started tutorial
  - Feature walkthroughs
  - Troubleshooting guide

- [ ] **Architecture Decision Records** - Document key technical decisions
  - Blazor vs Angular migration rationale
  - CQRS pattern implementation
  - Google Maps vs alternatives

---

**Note**: Items marked with priority (P0/P1/P2) should be prioritized accordingly. For contribution guidelines, see [CONTRIBUTING.md](../CONTRIBUTING.md).
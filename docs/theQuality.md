
## Testing Piramide

//TODO describe flow and use cases

## Unit tests



## Integration tests

Integration tests are made, as the name suggests, for integration. They may cover integration with APIs, external resources, storages and in specific - SQL database. SQL queries and commands often contain specific logic and mapping. In the case, all of the SQL operation should be properly tested. <b>SQL tests are covering business logic and this is why it's important to have them run in the build pipeline</b>.

<p>
Firstly, the correct configuration has to be setup:

```csharp
"ConnectionString": "Data Source=localhost,1401;Database=TaleCodeDatabase; User ID=SA;Password=password_xxddd_2137;Persist Security Info=True;MultipleActiveResultSets=True;Trusted_Connection=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=True"
```

Connection string points to local host on both: local and build (pipeline) environments.

The SQL Server is run in docker using following script:

```csharp
docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=password_xxddd_2137' -p 1401:1433 --name DB -d mcr.microsoft.com/mssql/server:2019-latest
dotnet publish /p:TargetServerName=localhost /p:TargetPort=1401 /p:TargetUser=sa /p:TargetPassword=password_xxddd_2137 /p:TargetDatabaseName=TaleCodeDatabase (from 'taleCode/src/TaleCodeDatabase' directory)
```

As you can see, the two prerequisites are needed in this configuration:
- docker installed on the build machine
- SQL proj containing database schema, which creates .dacpac file on build

Alternatively, the SQL Server itself could be installed on the build machine - it may requrie app settings change before running integration tests.
</p>

<p>
When the SQL Server is up and running, the tests itself has to be configured as well:

- run on clean database
- run sequentially

The SqlFixture is intrduced to fulfill this needs:

```csharp
    public class SqlFixture : IAsyncLifetime
    {
        public ISqlConnectionFactory SqlConnectionFactory;
        public SqlConnection SqlConnection { get; private set; }
        private string _connectionString;

        public async Task InitializeAsync()
        {
            var config = Options.Create(new SqlConfiguration
            {
                //could be fetched from appSettings or environmental variable
                ConnectionString =
                    "Data Source=localhost,1401;Database=TaleCodeDatabase; User ID=SA;Password=password_xxddd_2137;Persist Security Info=True;MultipleActiveResultSets=True;Trusted_Connection=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=True"
            });

            _connectionString = config.Value.ConnectionString;

            SqlConnectionFactory = new SqlConnectionFactory(config);

            SqlConnection?.Dispose();
            SqlConnection = new SqlConnection(_connectionString);
            SqlConnection.Open();

            await new Respawn.Checkpoint().Reset(_connectionString);
        }

        public async Task DisposeAsync()
        {
            SqlConnection?.Dispose();
            await new Respawn.Checkpoint().Reset(_connectionString);
        }
```
</p>

<p>
And example test itself:

```csharp
    private readonly PlayerRepository _sut;
        private readonly Fixture _fixture;

        public PlayerRepositoryTests(SqlFixture sqlFixture)
        {
            _fixture = new Fixture();
            _sut = new PlayerRepository(sqlFixture.SqlConnectionFactory);
        }

        [Fact]
        public void Insert_ValidPlayer_ItIsSavedInDB()
        {
            //Arrange

            var playerId = 123;

            var teams = _fixture.Build<Team>()
                .With(t => t.PlayerApiId, playerId)
                .CreateMany()
                .ToList();

            Player player = _fixture
                .Build<Player>()
                .With(p => p.ApiId, playerId)
                .With(p => p.DateOfBirth, DateTime.UtcNow.Date)
                .With(p => p.Teams, teams)
                .Create();

            //Act
            _sut.Insert(player);

            //Assert
            var result = _sut.GetById(playerId);

            Assert.NotNull(result);
            Assert.NotEmpty(result.Teams);

            result.DateOfBirth.Should().Be(player.DateOfBirth);
            result.Name.Should().Be(player.Name);
            result.Nationality.Should().Be(player.Nationality);
            result.Position.Should().Be(player.Position);
            result.Teams.Should().BeEquivalentTo(player.Teams, 
                config: options => options
                    .Excluding(a => a.Id)
                    .Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromSeconds(1))).WhenTypeIs<DateTime>());
        }
```
</p>



## Functional Tests

Functional tests are designed to test the entire feature in an isolated environment, ensuring that all components work together as expected. These tests simulate real-world scenarios and validate the system's behavior from the user's perspective.  

### Initialize the Environment:  
The IntegrationTestsFixture class is responsible for setting up the testing environment. It initializes the necessary components and dependencies, such as the API server and mock services.

```csharp
    [SetUpFixture]
    [SetCulture("en-US")]
    public static class IntegrationTestsFixture
    {
    public static ApiFixture<Program> ApiFixture { get; set; }
    public static ApiFixture<Worker.Program> WorkerFixture { get; set; }
    public static WireMockFixture WireMockFixture { get; set; }
    
        [OneTimeSetUp]
        public static void SetUp()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "development");
    
            ApiFixture = new ApiFixture<Program>();
            WorkerFixture = new ApiFixture<Worker.Program>();
    
            WireMockFixture = new WireMockFixture();
            WireMockFixture.Initialize();
            WireMockFixture.RegisterFakeApi(new GoogleFakeApi());
        }
    
        [OneTimeTearDown]
        public static void TearDown()
        {
            ApiFixture.Dispose();
            WorkerFixture.Dispose();
            WireMockFixture.Dispose();
        }
    }
```
### Define Test Scenarios:
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
            _wireMockFixture.Fake<IGoogleApiClient>()
                .WithRequest(x => x.GetLocationOfCity, city.Name)
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
        _wireMockFixture.Fake<IGoogleApiClient>()
            .WithRequest(x => x.GetDurationMatrixByFreeRoad, cities)
            .WithResponse(x => x.WithSuccess().WithBody(GoogleFakeApi.FreeDistanceMatrix));
    
        _wireMockFixture.Fake<IGoogleApiClient>()
            .WithRequest(x => x.GetDurationMatrixByTollRoad, cities)
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
    }
```

### Assert expected results:

```csharp
    apiResponse.IsSuccess.Should().BeTrue();
    var paths = apiResponse.Data.BestPaths;
    
    // Then the returned path is optimal
    paths[0].StartingCity.Name.Should().Be("Wroclaw");
    paths[0].EndingCity.Name.Should().Be("Vienna");
    
    paths[1].StartingCity.Name.Should().Be("Vienna");
    paths[1].EndingCity.Name.Should().Be("Firenze");
    
    paths[2].StartingCity.Name.Should().Be("Firenze");
    paths[2].EndingCity.Name.Should().Be("Barcelona");
```

This test verifies that the CalculateBestPath feature correctly finds the optimal path between a list of cities by interacting with the fake Google APIs. The test ensures that the entire feature works as expected in an isolated environment, providing confidence in the system's behavior.

## App Insights Logs
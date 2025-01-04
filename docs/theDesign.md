## Architecture


![design](./taleCodeArchitecture.png)


| Component  | Purpose  |   
|---|---|
| Background Worker  | Runs Commands on time/event/api trigger |     
| API                | Provides Queries for users  |   
| SQL Database       | Stores data in relational model  |   
| Service Bus        | Message Broker  |   
| External API       | Provides data needed by the Application  |   
| Application Insights  | Collects logs from all sources  |   



Application written in Tale Code uses the Clean Architecture approach. Projects are organized into layers with clear responsibilities and dependencies from top to bottom.

#### 1) Domain
The domain is the heart of the application. It identifies the area, bounded context, and common language between business and developers. The information is gathered into models understandable by every team member of the project.

#### 2) Data Layer
This layer is responsible for communication with data sources. The simplest use case would be a repository built on an SQL database or HTTP client. It may contain multiple projects dedicated to different types of sources.

#### 3) Logic Layer
The part for which stakeholders are paying. It contains only the pieces of code that bring business value. It does not care where the data comes from or where it ends. Its responsibility is to run all of the operations, calculations, commands, and queries required to achieve success. It might use CQRS, Domain Services, or Application approaches, whichever is the most extensible, natural, and understandable for the development team.

#### 4) Presentation Layer
The entry point or points for the application. API, triggers, scheduled tasks, event handlers - every way of making the application do something is considered as Presentation.

#### 5) Infrastructure
Everything else. Drivers, extensions, and the rest of the technical details needed to write a good piece of code.

![design](./Architecture.PNG)



## Code Design

### Presentation Layer

Starting from the top, Tale Code is organized in well-known and old-fashioned layers. The first and initial layer is the Presentation Layer.

This layer is responsible for communication with the outside world. It contains a minimum of code. Presentation is only a wrapper for the logic, containing all presentation-specific aspects (like authentication). In the sample app, it's built of 2 components:

#### BackgroundWorker

Subscribes to messages and invokes operations based on them. In this scenario, on CitySearched event invokes FetchCityDetails command:

```csharp
    public class FetchCityDetailsJob(IMediator mediator) : INotificationHandler<CitySearched>
    {
        public async Task Handle(CitySearched notification, CancellationToken cancellationToken)
        {
            await mediator.Send(new FetchCityDetailsCommand {Name = notification.Name}, cancellationToken);
        }
    }
```

#### Api

Provides a public interface for queries:

```csharp
    [Route(Route)]
    [ServiceFilter(typeof(ExceptionFilter))]
    [ServiceFilter(typeof(ResponseEnvelopeFilter))]
    public class FindCityByNameController(
        IMediator mediator,
        ILogger<FindCityByNameController> logger)
        : ControllerBase
    {
        public const string Route = "api/v2/FindCityByName";


        [HttpPost]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(Result<City>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(Result), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> FindCityByName([FromBody] FindCityByNameQuery query)
        {
            logger.LogInformation("Looking for city: " + query.Name);
            return Ok(await mediator.Send(query));
        }
    }
```



### Logic Layer

Contains all of the business knowledge and operations. Commands and Queries are separated from each other. They have totally different purposes, there is no reason to mix them. They can vary on everything, even on the technology used.

#### Commands

Commands are operations focused on data consistency. Execution time is not a crucial factor for commands, rather reliability. That's why I suggest following couple of simple rules:
1) Validate command input!
2) Ensure command could be retried
3) Ensure that no information is lost - data consistency and quality checks
4) Work on smart error handling - distinct between expected failure scenarios and exceptions
5) Log a lot, and have log scope


#### Queries

Queries are fundamentally different from commands. Crucial factor is the response time.
1) Be quick
2) No not rely on external services if possible
3) Use pre-generated read models
4) Be fast
5) Cache responses
6) Handle errors - fallbacks
7) Respond rapidly


#### Handlers Structure

Extending the Readme example:

```csharp
var context = new CalculateBestPathContext(cities!);

var result = await Chain
     .Start(context, cancellationToken)
     .Then(_downloadRoadData)
     .Then(_findProfitablePath)
     .Then(_solveTSP)
     .End(_formResult);
```


The repository is structurized in this way:

```
src/
├─ Presentation/
│  ├─ API
├─ Logic/
│  ├─ Commands/
│  │  ├─ X/
│  │  │  ├─ CommandX.cs
│  │  │  ├─ CommandXHandler.cs
│  │  │  ├─ CommandXContext.cs
│  │  │  ├─ CommandXResult.cs
│  │  │  ├─ Executors/
│  │  │  │  ├─ Action1.cs
│  │  │  │  ├─ Action2.cs
│  │  │  │  ├─ ...
│  ├─ Queries/
│  │  ├─ .../
├─ Data/
│  ├─ SQL/
│  ├─ .../
```


Where:

- Handler is already known to you - this is the business logic part
- Command is an input anemic model
- Result is an output of the operation
- Context holds the data needed for the whole operation. It is shared between Handler and Executors
- Executors are steps needed for fulfilling the business logic. They have a single and clear purpose

For maximum readability Handler behaves like an orchestrator. It calls the executors in needed order and produces the final result. The code is clear from the solution view and from the editor itself.


<img alt="design" src="./handlerStructure.JPG">



### Data Layer

Encapsulates all of the code needed for external components or services integration.

#### ApiClients

External API clients:

```csharp
    public partial class GoogleApiClient : IGoogleApiClient
    {
        private readonly GoogleApiOptions _options;
        private readonly ILogger<GoogleApiClient> _logger;
        private readonly HttpClient _httpClient;

        public GoogleApiClient(IOptions<GoogleApiOptions> options, HttpClient httpClient, ILogger<GoogleApiClient> logger)
        {
            _options = options.Value;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<City> GetLocationOfCity(string cityName)
        {
            try
            {
                City result = new City { Name = cityName };

                var request = await _httpClient
                    .CreateRequest($"maps/api/geocode/json?address={cityName}&key={_options.Key}")
                    .GetAsync();

                var response = await request.Content.ReadAsStringAsync();
                JObject json = JObject.Parse(response);

                result.Latitude = json["results"][0]["geometry"]["location"]["lat"].Value<double>();
                result.Longitude = json["results"][0]["geometry"]["location"]["lng"].Value<double>();
                return result;
            }

            catch (Exception)
            {
                throw new InvalidDataException($"Cannot find city [{cityName}]");
            }
        }
    }
```

#### SqlData

SQL repositiories and queries:


```csharp
    public partial class PlayerRepository : IPlayerRepository
    {
        private readonly ISqlConnectionFactory _sqlConnectionFactory;

        public PlayerRepository(ISqlConnectionFactory sqlConnectionFactory)
        {
            _sqlConnectionFactory = sqlConnectionFactory;
        }


        private const string InsertSql = @"
  INSERT INTO [dbo].[Player] (
    [ApiId],
	[Name],
	[DateOfBirth],
    [Nationality],
	[Position])
VALUES (@ApiId, @Name, @DateOfBirth, @Nationality, @Position)
";

        public void Insert(Player player)
        {
            using (var connection = _sqlConnectionFactory.CreateConnection())
            {
                var transaction = connection.BeginTransaction();

                connection.Execute(InsertSql, new
                {
                    ApiId = player.ApiId,
                    Name = player.Name,
                    DateOfBirth = player.DateOfBirth,
                    Nationality = player.Nationality,
                    Position = player.Position
                }, transaction);


                connection.Insert<Team>(player.Teams, transaction);

                transaction.Commit();
            }

        }
    }
```

What is worth to mention here. in purpose of keeping classes small and readabe, the adventage of partial classes can be used:

<img alt="design" src="./partialClasses.JPG">


#### BlobData

Methods for Blob Storage read/write operations:


```csharp
    public class PlayerStatisticsRepository : IPlayerStatisticsRepository
    {
        private const string ContainerName = "playerstatistics";

        private readonly BlobContainerClient _client;

        public PlayerStatisticsRepository(IBlobConnectionFactory blobConnectionFactory)
        {
            _client = blobConnectionFactory.CreateConnection(ContainerName);
        }

        public async Task Add(PlayerStatistics playerStatistics)
        {
            await _client.WriteToBlob(playerStatistics.Id.ToString(), playerStatistics);
        }

        public async Task<PlayerStatistics> Get(int playerId)
        {
            var result = await _client.ReadFromBlob<PlayerStatistics>(playerId.ToString());

            return result;
        }
    }
```

#### StaticData

Contains data not changing in time.

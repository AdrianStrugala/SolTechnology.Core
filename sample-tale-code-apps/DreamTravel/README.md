# Dream Travel

*"Observing the past centuries we can conclude, that the humanity tend to search for optimal roads. Merchants, travelers, and explorers were creating more and more detailed maps. In the digital era, everyone can use free and open real-time maps found in the Internet. Some of them provide a feature for finding the shortest road connecting a set of cities, but very few can answer common question “Is it really worth to pay for toll highway or should I just stay on the free road?” A solution of Traveling Eco-Salesman Problem provides an answer to this question.
<br>
 Program is written in C#, implements Ant Colony Optimization and God alorithms."*


 That's the original Readme of Dream Travel app, written in ~2016 as part of my master thesis. Feeling old now.
 
 ### Purpose

 The Dream Trvel app contains two parts: frontend page where user can set up a set of cities. And backend which is sorting those cities according to custom formula binding time and cost of the travel (Traveling Salesman Problem solver).

 ### Design

 For couple of years DT was my pet project, where I was testing out new patterns and code designs. That's why I am going to be quite detailed in this section.

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
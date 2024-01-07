using DreamTravel.TravelingSalesmanProblem;
using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Queries.CalculateBestPath.Executors;
using Polly;
using SolTechnology.Core.CQRS;
using Path = DreamTravel.Trips.Domain.Paths.Path;

namespace DreamTravel.Trips.Queries.CalculateBestPath;

public class CalculateBestPathHandler : IQueryHandler<CalculateBestPathQuery, CalculateBestPathResult>
{
    private readonly Func<CalculateBestPathContext, Task> _downloadRoadData;
    private readonly Func<List<City>, CalculateBestPathContext, List<int>, List<Path>> _formPathsFromMatrices;
    private readonly Action<CalculateBestPathContext> _findProfitablePath;
    private readonly Action<CalculateBestPathContext> _solveTSP;

    public CalculateBestPathHandler(
        IDownloadRoadData downloadRoadData,
        IFormPathsFromMatrices formPathsFromMatrices, ISolveTsp solveTsp, IFindProfitablePath findProfitablePath)
    {
        _downloadRoadData = downloadRoadData.Execute;
        _formPathsFromMatrices = formPathsFromMatrices.Execute;
        _solveTSP = solveTsp.Execute;
        _findProfitablePath = findProfitablePath.Execute;
    }

    public async Task<CalculateBestPathResult> Handle(CalculateBestPathQuery query)
    {
        var cities = query.Cities.Where(c => c != null).ToList();
        var context = new CalculateBestPathContext(cities!);


        await Chain2
            .Start(context)
            .Next(_downloadRoadData)
            .Next(_findProfitablePath)
            .Next(_solveTSP);

        CalculateBestPathResult calculateBestPathResult = new CalculateBestPathResult
        {
            Cities = cities!,
            BestPaths = _formPathsFromMatrices(cities!, context, context.OrderOfCities)
        };
        return calculateBestPathResult;
    }
}

public static class Chain2
{
    public static Chain2<TContext> Start<TContext>(TContext context)
    {
        return new Chain2<TContext>(context);
    }
}


public class Chain2<TContext>
{
    private TContext Context { get; }

    public Chain2(TContext context)
    {
        Context = context;
    }

    public Chain2<TContext> Next(Action<TContext> action)
    {
        action.Invoke(Context);
        return this;
    }

    public async Task<Chain2<TContext>> Next(Func<TContext, Task> func)
    {
        await func.Invoke(Context);
        return this;
    }
}

public static class Chain2Extensions
{
    public static async Task<Chain2<TContext>> Next<TContext>(
        this Task<Chain2<TContext>> asyncChain,
        Action<TContext> action)
    {
        var chain = await asyncChain;
        return chain.Next(action);
    }
}
using DreamTravel.Trips.Queries.CalculateBestPath.Executors;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.CQRS.Operations;

namespace DreamTravel.Trips.Queries.CalculateBestPath;

public class CalculateBestPathHandler : IQueryHandler<CalculateBestPathQuery, CalculateBestPathResult>
{
    private readonly Func<CalculateBestPathContext, Task<OperationResult>> _downloadRoadData;
    private readonly Func<CalculateBestPathContext, Task<OperationResult>> _findProfitablePath;
    private readonly Func<CalculateBestPathContext, Task<OperationResult>> _solveTSP;
    private readonly Func<CalculateBestPathContext, CalculateBestPathResult> _formResult;

    public CalculateBestPathHandler(
        IDownloadRoadData downloadRoadData,
        IFormCalculateBestPathResult formCalculateBestPathResult, ISolveTsp solveTsp, IFindProfitablePath findProfitablePath)
    {
        _downloadRoadData = downloadRoadData.Execute;
        _formResult = formCalculateBestPathResult.Execute;
        _solveTSP = solveTsp.Execute;
        _findProfitablePath = findProfitablePath.Execute;
    }


    public async Task<CalculateBestPathResult> Handle(CalculateBestPathQuery query, CancellationToken cancellationToken = default)
    {
        var cities = query.Cities.Where(c => c != null).ToList();
        var context = new CalculateBestPathContext(cities!);

        var result = await Chain2
             .Start(context, cancellationToken)
             .Then(_downloadRoadData)
             .Then(_findProfitablePath)
             .Then(_solveTSP)
             .End(_formResult);

        return result;
    }
}

public static class Chain2
{
    public static Chain2<TContext> Start<TContext>(TContext context, CancellationToken cancellationToken)
    {
        return new Chain2<TContext>(context, cancellationToken);
    }
}


public class Chain2<TContext>
{
    private readonly CancellationToken _cancellationToken;
    private readonly List<Exception> _exceptions = new();
    internal TContext Context { get; }



    public Chain2(TContext context, CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        Context = context;
    }

    public async Task<Chain2<TContext>> Then(Func<TContext, Task<OperationResult>> func)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        if (_exceptions.Any())
        {
            return this;
        }

        var operationResult = new OperationResult();
        try
        {
            operationResult = await func.Invoke(Context);
        }
        catch (Exception e)
        {
            _exceptions.Add(e);
        }
        if (operationResult.IsFailed)
        {
            _exceptions.Add(new Exception(operationResult.ErrorMessage));
        }
        return this;
    }

    public TResult End<TResult>(Func<TContext, TResult> func)
    {
        return func.Invoke(Context);
    }
}

public static class Chain2Extensions
{
    public static async Task<Chain2<TContext>> Then<TContext>(
        this Task<Chain2<TContext>> asyncChain,
        Func<TContext, Task<OperationResult>> action)
    {
        var chain = await asyncChain;
        return await chain.Then(action);
    }

    public static async Task<TResult> End<TContext, TResult>(
        this Task<Chain2<TContext>> asyncChain,
        Func<TContext, TResult> action)
    {
        var chain = await asyncChain;
        return chain.End(action);
    }
}
using DreamTravel.Trips.Queries.CalculateBestPath.Executors;
using SolTechnology.Core.CQRS;

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


    public async Task<OperationResult<CalculateBestPathResult>> Handle(CalculateBestPathQuery query, CancellationToken cancellationToken = default)
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

using DreamTravel.TravelingSalesmanProblem;
using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Queries.CalculateBestPath.Executors;
using SolTechnology.Core.CQRS;
using Path = DreamTravel.Trips.Domain.Paths.Path;

namespace DreamTravel.Trips.Queries.CalculateBestPath;

public class CalculateBestPathHandler : IQueryHandler<CalculateBestPathQuery, CalculateBestPathResult>
{
    private readonly Func<List<City>, CalculateBestPathContext, Task> _downloadRoadData;
    private readonly Func<List<City>, CalculateBestPathContext, List<int>, List<Path>> _formPathsFromMatrices;
    private readonly Func<List<double>, List<int>> _solveTSP;
    private readonly Action<CalculateBestPathContext, int> _findProfitablePath;

    public CalculateBestPathHandler(IDownloadRoadData downloadRoadData, IFormPathsFromMatrices formPathsFromMatrices, ITSP solveTsp, IFindProfitablePath findProfitablePath)
    {
        _downloadRoadData = downloadRoadData.Execute;
        _formPathsFromMatrices = formPathsFromMatrices.Execute;
        _solveTSP = solveTsp.SolveTSP;
        _findProfitablePath = findProfitablePath.Execute;
    }

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
}
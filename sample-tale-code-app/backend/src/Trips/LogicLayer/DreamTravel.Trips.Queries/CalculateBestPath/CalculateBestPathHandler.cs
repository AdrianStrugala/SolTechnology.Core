using DreamTravel.TravelingSalesmanProblem;
using DreamTravel.Trips.Queries.CalculateBestPath.Executors;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Trips.Queries.CalculateBestPath
{
    public class CalculateBestPathHandler : IQueryHandler<CalculateBestPathQuery, CalculateBestPathResult>
    {
        private readonly IDownloadRoadData _downloadRoadData;
        private readonly IFormPathsFromMatrices _formPathsFromMatrices;
        private readonly ITSP _tspSolver;
        private readonly IFindProfitablePath _findProfitablePath;

        public CalculateBestPathHandler(IDownloadRoadData downloadRoadData, IFormPathsFromMatrices formPathsFromMatrices, ITSP tspSolver, IFindProfitablePath findProfitablePath)
        {
            _downloadRoadData = downloadRoadData;
            _formPathsFromMatrices = formPathsFromMatrices;
            _tspSolver = tspSolver;
            _findProfitablePath = findProfitablePath;
        }

        public async Task<CalculateBestPathResult> Handle(CalculateBestPathQuery query)
        {
            var cities = query.Cities.Where(c => c != null).ToList();
            var context = new CalculateBestPathContext(cities.Count);

            await _downloadRoadData.Execute(cities!, context);
            _findProfitablePath.Execute(context, cities.Count);

            var orderOfCities = _tspSolver.SolveTSP(context.OptimalDistances.ToList());

            CalculateBestPathResult calculateBestPathResult = new CalculateBestPathResult
            {
                Cities = cities!,
                BestPaths = _formPathsFromMatrices.Execute(cities!, context, orderOfCities)
            };
            return calculateBestPathResult;
        }
    }
}

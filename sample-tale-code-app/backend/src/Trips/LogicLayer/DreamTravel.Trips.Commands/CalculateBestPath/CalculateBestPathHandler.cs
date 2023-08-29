using System.Linq;
using System.Threading.Tasks;
using DreamTravel.DreamTrips.CalculateBestPath.Interfaces;
using DreamTravel.Infrastructure;
using DreamTravel.TravelingSalesmanProblem;
using DreamTravel.Trips.Domain.Cities;

namespace DreamTravel.DreamTrips.CalculateBestPath
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

            EvaluationMatrix evaluationMatrix = await _downloadRoadData.Execute(cities);
            evaluationMatrix = _findProfitablePath.Execute(evaluationMatrix, cities.Count);

            var orderOfCities = _tspSolver.SolveTSP(evaluationMatrix.OptimalDistances.ToList());

            //to have a possiblity to store cities data
            // File.WriteAllText("./xCities.txt", JsonConvert.SerializeObject(evaluationMatrix.OptimalDistances));

            CalculateBestPathResult calculateBestPathResult = new CalculateBestPathResult
            {
                Cities = cities,
                BestPaths = _formPathsFromMatrices.Execute(cities, evaluationMatrix, orderOfCities)
            };
            return calculateBestPathResult;
        }
    }
}

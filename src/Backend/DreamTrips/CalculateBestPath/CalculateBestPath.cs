using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DreamTravel.Domain.Cities;
using DreamTravel.DreamTrips.CalculateBestPath.Interfaces;
using DreamTravel.DreamTrips.CalculateBestPath.Models;
using DreamTravel.TravelingSalesmanProblem;

namespace DreamTravel.DreamTrips.CalculateBestPath
{
    public class CalculateBestPath : ICalculateBestPath
    {
        private readonly IDownloadRoadData _downloadRoadData;
        private readonly IFormPathsFromMatrices _formPathsFromMatrices;
        private readonly ITSP _tspSolver;
        private readonly IFindProfitablePath _findProfitablePath;

        public CalculateBestPath(IDownloadRoadData downloadRoadData, IFormPathsFromMatrices formPathsFromMatrices, ITSP tspSolver, IFindProfitablePath findProfitablePath)
        {
            _downloadRoadData = downloadRoadData;
            _formPathsFromMatrices = formPathsFromMatrices;
            _tspSolver = tspSolver;
            _findProfitablePath = findProfitablePath;
        }

        public async Task<Result> Execute(List<City> cities, bool optimizePath)
        {
            EvaluationMatrix evaluationMatrix = new EvaluationMatrix(cities.Count);
            evaluationMatrix = await _downloadRoadData.Execute(cities, evaluationMatrix);
            evaluationMatrix = _findProfitablePath.Execute(evaluationMatrix, cities.Count);


            List<int> orderOfCities;
            if (optimizePath)
            {
                orderOfCities = _tspSolver.SolveTSP(evaluationMatrix.OptimalDistances.ToList());
            }
            else
            {
                orderOfCities = Enumerable.Range(0, cities.Count).ToList();
            }


            //to have a possiblity to store cities data
            // File.WriteAllText("./xCities.txt", JsonConvert.SerializeObject(evaluationMatrix.OptimalDistances));

            Result result = new Result
            {
                Cities = cities,
                BestPaths = _formPathsFromMatrices.Execute(cities, evaluationMatrix, orderOfCities)
            };
            return result;
        }

    }
}

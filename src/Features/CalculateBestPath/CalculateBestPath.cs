using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DreamTravel.Domain.Cities;
using DreamTravel.Features.CalculateBestPath.Interfaces;
using DreamTravel.Features.CalculateBestPath.Models;
using DreamTravel.TravelingSalesmanProblem;

namespace DreamTravel.Features.CalculateBestPath
{
    public class CalculateBestPath : ICalculateBestPath
    {
        private readonly IDownloadRoadData _downloadRoadData;
        private readonly IFormOutputData _formOutputData;
        private readonly ITSP _tspSolver;
        private readonly IFindProfitablePath _findProfitablePath;

        public CalculateBestPath(IDownloadRoadData downloadRoadData, IFormOutputData formOutputData, ITSP tspSolver, IFindProfitablePath findProfitablePath)
        {
            _downloadRoadData = downloadRoadData;
            _formOutputData = formOutputData;
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
                BestPaths = _formOutputData.Execute(cities, evaluationMatrix, orderOfCities)
            };
            return result;
        }

    }
}

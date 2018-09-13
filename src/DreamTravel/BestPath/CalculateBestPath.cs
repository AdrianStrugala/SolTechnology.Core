using System.Collections.Generic;
using DreamTravel.BestPath.Interfaces;
using DreamTravel.BestPath.Models;
using DreamTravel.ExternalConnection.Interfaces;
using DreamTravel.SharedModels;
using TravelingSalesmanProblem;
using Path = DreamTravel.SharedModels.Path;

namespace DreamTravel.BestPath
{
    public class CalculateBestPath : ICalculateBestPath
    {
        private readonly IDownloadRoadData _downloadRoadData;
        private readonly IFormOutputData _formOutputData;
        private readonly ITSP _tspSolver;
        private readonly IEvaluationBrain _evaluationBrain;

        public CalculateBestPath(IDownloadRoadData downloadRoadData, IFormOutputData formOutputData, ITSP tspSolver, IEvaluationBrain evaluationBrain)
        {
            _downloadRoadData = downloadRoadData;
            _formOutputData = formOutputData;
            _tspSolver = tspSolver;
            _evaluationBrain = evaluationBrain;
        }

        public List<Path> Execute(List<City> cities)
        {
            EvaluationMatrix matrices = new EvaluationMatrix(cities.Count);
            matrices = _downloadRoadData.Execute(cities, matrices);
            matrices = _evaluationBrain.Execute(matrices, cities.Count);
            int[] orderOfCities = _tspSolver.SolveTSP(matrices.OptimalDistances);

            //to have a possiblity to store cities data
            // File.WriteAllText("./twentyCities.txt", JsonConvert.SerializeObject(matrices.OptimalDistances));

            return _formOutputData.Execute(cities, orderOfCities, matrices);
        }

    }
}

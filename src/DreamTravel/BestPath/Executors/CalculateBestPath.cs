namespace DreamTravel.BestPath.Executors
{
    using Interfaces;
    using Models;
    using SharedModels;
    using System.Collections.Generic;
    using System.Linq;
    using TravelingSalesmanProblem;

    public class CalculateBestPath : ICalculateBestPath
    {
        private readonly IDownloadRoadData _downloadRoadData;
        private readonly IFormOutputData _formOutputData;
        private readonly ITSP _tspSolver;
        private readonly IEvaluationBrain _evaluationBrain;
        private IIdentifyUnknownCities _identifyUnknownCities;

        public CalculateBestPath(IDownloadRoadData downloadRoadData, IFormOutputData formOutputData, ITSP tspSolver, IEvaluationBrain evaluationBrain, IIdentifyUnknownCities identifyUnknownCities)
        {
            _downloadRoadData = downloadRoadData;
            _formOutputData = formOutputData;
            _tspSolver = tspSolver;
            _evaluationBrain = evaluationBrain;
            _identifyUnknownCities = identifyUnknownCities;
        }

        public Result Execute(Command command)
        {
            List<City> newCities = _identifyUnknownCities.Execute(command.Cities, command.KnownCities);

            EvaluationMatrix matrices = new EvaluationMatrix(command.Cities.Count);
            matrices = _downloadRoadData.Execute(command.Cities, matrices);
            matrices = _evaluationBrain.Execute(matrices, command.Cities.Count);

            List<int> orderOfCities;
            if (command.OptimizePath)
            {
                orderOfCities = _tspSolver.SolveTSP(matrices.OptimalDistances.ToList());
            }
            else
            {
                orderOfCities = Enumerable.Range(0, command.Cities.Count).ToList();
            }


            //to have a possiblity to store cities data
            // File.WriteAllText("./twentyCities.txt", JsonConvert.SerializeObject(matrices.OptimalDistances));

            Result result = new Result
            {
                Cities = command.Cities,
                AllPaths = _formOutputData.Execute(command.Cities, matrices),
                BestPaths = _formOutputData.Execute(command.Cities, matrices, orderOfCities)
            };
            return result;
        }

    }
}

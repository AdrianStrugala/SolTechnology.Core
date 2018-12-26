namespace DreamTravel.BestPath.Executors
{
    using Interfaces;
    using Models;
    using System.Collections.Generic;
    using System.Linq;
    using TravelingSalesmanProblem;

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

        public Result Execute(Command command)
        {
            //TODO Reduce number of cities to download data by already known
            

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

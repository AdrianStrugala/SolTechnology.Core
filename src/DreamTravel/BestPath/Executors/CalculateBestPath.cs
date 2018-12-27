namespace DreamTravel.BestPath.Executors
{
    using Interfaces;
    using Models;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
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

        public async Task<Result> Execute(Command command)
        {
            EvaluationMatrix evaluationMatrix = new EvaluationMatrix(command.Cities.Count);
            evaluationMatrix = await _downloadRoadData.Execute(command.Cities, evaluationMatrix);
            evaluationMatrix = _evaluationBrain.Execute(evaluationMatrix, command.Cities.Count);


            List<int> orderOfCities;
            if (command.OptimizePath)
            {
                orderOfCities = _tspSolver.SolveTSP(evaluationMatrix.OptimalDistances.ToList());
            }
            else
            {
                orderOfCities = Enumerable.Range(0, command.Cities.Count).ToList();
            }


            //to have a possiblity to store cities data
            // File.WriteAllText("./xCities.txt", JsonConvert.SerializeObject(evaluationMatrix.OptimalDistances));

            Result result = new Result
            {
                Cities = command.Cities,
                BestPaths = _formOutputData.Execute(command.Cities, evaluationMatrix, orderOfCities)
            };
            return result;
        }

    }
}

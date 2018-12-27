namespace DreamTravel.BestPath.Executors
{
    using Interfaces;
    using Models;
    using SharedModels;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using TravelingSalesmanProblem;

    public class CalculateBestPath : ICalculateBestPath
    {
        private readonly IDownloadRoadData _downloadRoadData;
        private readonly IFormOutputData _formOutputData;
        private readonly ITSP _tspSolver;
        private readonly IEvaluationBrain _evaluationBrain;
        private readonly IIdentifyUnknownCities _identifyUnknownCities;

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

            List<Path> allPaths = new List<Path>();
            //            allPaths.AddRange(command.KnownPaths);

            Stopwatch s = Stopwatch.StartNew();
            foreach (var newCity in newCities)
            {

                allPaths.AddRange(_downloadRoadData.ExecuteV2(newCity, command.Cities));
            }

            var xd = s.ElapsedMilliseconds;


            //            var invertPaths = new List<Path>();
            //            foreach (var path in allPaths)
            //            {
            //                City pivot = path.StartingCity;
            //                path.StartingCity = path.EndingCity;
            //                path.EndingCity = pivot;
            //
            //                invertPaths.Add(path);
            //            }
            //
            //            allPaths.AddRange(invertPaths);

            EvaluationMatrix matrices = new EvaluationMatrix(command.Cities.Count);

            Stopwatch s2 = Stopwatch.StartNew();
            matrices = _downloadRoadData.Execute(command.Cities, matrices);
            var xd2 = s2.ElapsedMilliseconds;

            EvaluationMatrix matricesv3 = new EvaluationMatrix(command.Cities.Count);
            Stopwatch s3 = Stopwatch.StartNew();
            matricesv3 = _downloadRoadData.ExecuteV3(command.Cities, matricesv3);
            var xd3 = s3.ElapsedMilliseconds;

            EvaluationMatrix matricesv4 = new EvaluationMatrix(command.Cities.Count);
            Stopwatch s4 = Stopwatch.StartNew();
            matricesv4 = _downloadRoadData.ExecuteV4(command.Cities, matricesv3);
            var xd4 = s4.ElapsedMilliseconds;


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

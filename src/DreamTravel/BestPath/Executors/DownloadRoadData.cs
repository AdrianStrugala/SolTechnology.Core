namespace DreamTravel.BestPath.Executors
{
    using Interfaces;
    using Models;
    using SharedModels;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class DownloadRoadData : IDownloadRoadData
    {
        private readonly IDownloadDurationMatrixByTollRoad _downloadDurationMatrixByTollRoad;
        private readonly IDownloadCostBetweenTwoCities _downloadCostBetweenTwoCities;
        private readonly IDownloadDurationMatrixByFreeRoad _downloadDurationMatrixByFreeRoad;

        public DownloadRoadData(IDownloadDurationMatrixByTollRoad downloadDurationMatrixByTollRoad,
                                IDownloadDurationMatrixByFreeRoad downloadDurationMatrixByFreeRoad,
                                IDownloadCostBetweenTwoCities downloadCostBetweenTwoCities)
        {
            _downloadDurationMatrixByTollRoad = downloadDurationMatrixByTollRoad;
            _downloadDurationMatrixByFreeRoad = downloadDurationMatrixByFreeRoad;
            _downloadCostBetweenTwoCities = downloadCostBetweenTwoCities;
        }

        public EvaluationMatrix Execute(List<City> listOfCities,
            EvaluationMatrix evaluationMatrix)
        {
            SetTablesValueAsMax(evaluationMatrix, 0);

            Parallel.Invoke
            (
                () => evaluationMatrix.TollDistances = _downloadDurationMatrixByTollRoad.Execute(listOfCities),
                () => evaluationMatrix.FreeDistances = _downloadDurationMatrixByFreeRoad.Execute(listOfCities)
            );


            Parallel.For(0, listOfCities.Count, i =>
            {
                for (int j = 0; j < listOfCities.Count; j++)
                {
                    int iterator = j + i * listOfCities.Count;

                    if (i == j)
                    {
                        SetTablesValueAsMax(evaluationMatrix, iterator);
                    }

                    else
                    {
                        (evaluationMatrix.Costs[iterator], evaluationMatrix.VinietaCosts[iterator]) =
                            _downloadCostBetweenTwoCities.Execute(listOfCities[i], listOfCities[j]);
                    }
                }
            });

            return evaluationMatrix;
        }


        public async Task<EvaluationMatrix> ExecuteV4(List<City> listOfCities,
            EvaluationMatrix evaluationMatrix)
        {
            SetTablesValueAsMax(evaluationMatrix, 0);

            //            Parallel.Invoke
            //            (
            //                () => evaluationMatrix.TollDistances = _downloadDurationMatrixByTollRoad.Execute(listOfCities),
            //                () => evaluationMatrix.FreeDistances = _downloadDurationMatrixByFreeRoad.Execute(listOfCities)
            //            );

            var tasks = new List<Task>();
            tasks.Add(new Task(async () => evaluationMatrix.TollDistances = await _downloadDurationMatrixByTollRoad.ExecuteV4(listOfCities)));
          //  tasks.Add(new Task(() => evaluationMatrix.FreeDistances = _downloadDurationMatrixByFreeRoad.ExecuteV4(listOfCities)));

            await Task.WhenAll(tasks);


            Parallel.For(0, listOfCities.Count, i =>
            {
                for (int j = 0; j < listOfCities.Count; j++)
                {
                    int iterator = j + i * listOfCities.Count;

                    if (i == j)
                    {
                        SetTablesValueAsMax(evaluationMatrix, iterator);
                    }

                    else
                    {
                        (evaluationMatrix.Costs[iterator], evaluationMatrix.VinietaCosts[iterator]) =
                            _downloadCostBetweenTwoCities.ExecuteV4(listOfCities[i], listOfCities[j]);
                    }
                }
            });

            return evaluationMatrix;
        }


        public List<Path> ExecuteV2(City origin, List<City> destinations)
        {
            Path[] result = new Path[destinations.Count];


            double[] tollDistances = new double[destinations.Count];
            double[] freeDistances = new double[destinations.Count];

            Parallel.Invoke
            (
                () => tollDistances = _downloadDurationMatrixByTollRoad.ExecuteV2(origin, destinations),
                () => freeDistances = _downloadDurationMatrixByFreeRoad.ExecuteV2(origin, destinations)
            );


            for (int i = 0; i < destinations.Count; i++)
            // Parallel.For(0, destinations.Count, i =>
            {
                Path pathToAdd = new Path();

                pathToAdd.StartingCity = origin;
                pathToAdd.EndingCity = destinations[i];

                if (pathToAdd.StartingCity.Name != pathToAdd.EndingCity.Name)
                {
                    (double cost, double vinietaCost) = _downloadCostBetweenTwoCities.Execute(pathToAdd.StartingCity, pathToAdd.EndingCity);

                    pathToAdd.Cost = cost;
                    pathToAdd.VinietaCost = vinietaCost;
                    pathToAdd.FreeDistance = freeDistances[i];
                    pathToAdd.TollDistance = tollDistances[i];
                }
                else
                {
                    SetPathValuesAsMax(pathToAdd);
                }

                result[i] = pathToAdd;
            }


            return result.ToList();
        }

        private void SetPathValuesAsMax(Path path)
        {
            path.FreeDistance = double.MaxValue;
            path.TollDistance = double.MaxValue;
            path.OptimalDistance = double.MaxValue;
            path.Goal = double.MaxValue;
            path.Cost = double.MaxValue;
            path.OptimalCost = double.MaxValue;
        }

        private static void SetTablesValueAsMax(EvaluationMatrix evaluationMatrix, int iterator)
        {
            evaluationMatrix.FreeDistances[iterator] = double.MaxValue;
            evaluationMatrix.TollDistances[iterator] = double.MaxValue;
            evaluationMatrix.OptimalDistances[iterator] = double.MaxValue;
            evaluationMatrix.Goals[iterator] = double.MaxValue;
            evaluationMatrix.Costs[iterator] = double.MaxValue;
            evaluationMatrix.OptimalCosts[iterator] = double.MaxValue;
        }
    }
}
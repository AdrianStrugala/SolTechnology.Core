namespace DreamTravel.WebUI.BestPath.Executors
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Contract;
    using Interfaces;
    using Models;

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

        public async Task<EvaluationMatrix> Execute(List<City> listOfCities,
            EvaluationMatrix evaluationMatrix)
        {
            SetTablesValueAsMax(evaluationMatrix, 0);

            List<Task> tasks = new List<Task>
            {
                Task.Run(async () => evaluationMatrix.TollDistances = await _downloadDurationMatrixByTollRoad.Execute(listOfCities)),
                Task.Run(async () => evaluationMatrix.FreeDistances = await _downloadDurationMatrixByFreeRoad.Execute(listOfCities))
            };
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
                            _downloadCostBetweenTwoCities.Execute(listOfCities[i], listOfCities[j]);
                    }
                }
            });

            return evaluationMatrix;
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
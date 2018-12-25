namespace DreamTravel.BestPath.Executors
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Interfaces;
    using Models;
    using SharedModels;

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
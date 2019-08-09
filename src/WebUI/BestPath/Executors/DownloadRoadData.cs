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

        public async Task<EvaluationMatrix> Execute(List<City> listOfCities, EvaluationMatrix evaluationMatrix)
        {
            List<Task> tasks = new List<Task>
            {
                Task.Run(async () => evaluationMatrix.TollDistances = await _downloadDurationMatrixByTollRoad.Execute(listOfCities)),
                Task.Run(async () => evaluationMatrix.FreeDistances = await _downloadDurationMatrixByFreeRoad.Execute(listOfCities)),
                Task.Run(() => { DownloadCostMatrix(listOfCities, evaluationMatrix); })
            };
            await Task.WhenAll(tasks);

            return evaluationMatrix;
        }

        private void DownloadCostMatrix(List<City> listOfCities, EvaluationMatrix evaluationMatrix)
        {
            Parallel.For(0, listOfCities.Count, new ParallelOptions
            {
                MaxDegreeOfParallelism = 10
            }, i =>
                {
                    for (int j = 0; j < listOfCities.Count; j++)
                    {
                        int iterator = j + i * listOfCities.Count;

                        (evaluationMatrix.Costs[iterator], evaluationMatrix.VinietaCosts[iterator]) =
                            _downloadCostBetweenTwoCities.Execute(listOfCities[i], listOfCities[j]);
                    }
                });
        }
    }
}
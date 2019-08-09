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
                Task.Run(async () => evaluationMatrix.FreeDistances = await _downloadDurationMatrixByFreeRoad.Execute(listOfCities))
            };

            tasks.AddRange(DownloadCostMatrix(listOfCities, evaluationMatrix));

            await Task.WhenAll(tasks);

            return evaluationMatrix;
        }

        private List<Task> DownloadCostMatrix(List<City> listOfCities, EvaluationMatrix evaluationMatrix)
        {
            List<Task> tasks = new List<Task>();

            for (int i = 0; i < listOfCities.Count; i++)
            {
                for (int j = 0; j < listOfCities.Count; j++)
                {
                    int iterator = j + i * listOfCities.Count;

                    var i1 = i;
                    var j1 = j;
                    tasks.Add(Task.Run(async () => (evaluationMatrix.Costs[iterator], evaluationMatrix.VinietaCosts[iterator]) =
                                                   await _downloadCostBetweenTwoCities.Execute(listOfCities[i1], listOfCities[j1])));
                }
            }

            return tasks;
        }
    }
}
using DreamTravel.GeolocationData;
using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Queries.CalculateBestPath.Interfaces;

namespace DreamTravel.Trips.Queries.CalculateBestPath.Executors
{
    public class DownloadRoadData : IDownloadRoadData
    {
        private readonly IGoogleApiClient _googleApiClient;
        private readonly IMichelinApiClient _michelinApiClient;

        public DownloadRoadData(IGoogleApiClient googleApiClient, IMichelinApiClient michelinApiClient)
        {
            _googleApiClient = googleApiClient;
            _michelinApiClient = michelinApiClient;
        }

        public async Task<EvaluationMatrix> Execute(List<City> listOfCities)
        {
            EvaluationMatrix evaluationMatrix = new EvaluationMatrix(listOfCities.Count);

            List<Task> tasks = new List<Task>
            {
                Task.Run(async () => evaluationMatrix.TollDistances = await _googleApiClient.GetDurationMatrixByTollRoad(listOfCities)),
                Task.Run(async () => evaluationMatrix.FreeDistances = await _googleApiClient.GetDurationMatrixByFreeRoad(listOfCities))
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
                                                   await _michelinApiClient.DownloadCostBetweenTwoCities(listOfCities[i1], listOfCities[j1])));
                }
            }

            return tasks;
        }
    }
}
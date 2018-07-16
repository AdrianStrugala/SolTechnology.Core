using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DreamTravel.Models;

namespace DreamTravel.ExternalConnection
{
    public class ProcessInputData : IProcessInputData
    {
        private readonly ICallAPI _apiCaller;

        public ProcessInputData(ICallAPI apiCaller)
        {
            _apiCaller = apiCaller;
        }

        public async Task<EvaluationMatrix> DownloadExternalData(List<City> listOfCities,
            EvaluationMatrix evaluationMatrix)
        {
            await listOfCities.Count.ForEachAsync(async i =>
            {
                await listOfCities.Count.ForEachAsync(async j =>
                {
                    int iterator = j + i * listOfCities.Count;

                    if (i == j)
                    {
                        SetTablesValueAsMax(evaluationMatrix, iterator);
                    }

                    else
                    {
                        Task freeDistancesCaller = Task.Run(async () => evaluationMatrix.FreeDistances[iterator] =
                            await GetDurationBetweenTwoCitiesByFreeRoad(listOfCities[i], listOfCities[j]));

                        Task tollDistancesCaller = Task.Run(async () => evaluationMatrix.TollDistances[iterator] =
                            await GetDurationBetweenTwoCitiesByTollRoad(listOfCities[i], listOfCities[j]));

                        Task costCaller = Task.Run(async () => evaluationMatrix.Costs[iterator] =
                            await GetCostBetweenTwoCities(listOfCities[i], listOfCities[j]));

                        await Task.WhenAll(freeDistancesCaller, tollDistancesCaller, costCaller);
                    }
                });
            });

            return evaluationMatrix;
        }

        private static void SetTablesValueAsMax(EvaluationMatrix evaluationMatrix, int iterator)
        {
            evaluationMatrix.FreeDistances[iterator] = Double.MaxValue;
            evaluationMatrix.TollDistances[iterator] = Double.MaxValue;
            evaluationMatrix.OptimalDistances[iterator] = Double.MaxValue;
            evaluationMatrix.Goals[iterator] = Double.MaxValue;
            evaluationMatrix.Costs[iterator] = Double.MaxValue;
            evaluationMatrix.OptimalCosts[iterator] = Double.MaxValue;
        }

        public List<string> ReadCities(string incomingCities)
        {
            string[] cities = incomingCities.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            );
            return cities.Where(x => !String.IsNullOrEmpty(x)).ToList();
        }

        public async Task<List<City>> GetCitiesFromGoogleApi(List<string> cityNames)
        {
            List<City> cities = new List<City>();
            foreach (var cityName in cityNames)
            {
                City downloadedCity = await _apiCaller.DownloadLocationOfCity(cityName);
                cities.Add(downloadedCity);
            }

            return cities;
        }

        public async Task<double> GetCostBetweenTwoCities(City origin, City destination)
        {
            var costInCents = await _apiCaller.DowloadCostBetweenTwoCities(origin, destination);
            return costInCents / 100;
        }

        public async Task<int> GetDurationBetweenTwoCitiesByTollRoad(City origin, City destination)
        {
            return await _apiCaller.DowloadDurationBetweenTwoCitesByTollRoad(origin, destination);
        }

        public async Task<int> GetDurationBetweenTwoCitiesByFreeRoad(City origin, City destination)
        {
            return await _apiCaller.DowloadDurationBetweenTwoCitesByFreeRoad(origin, destination);
        }
    }
}
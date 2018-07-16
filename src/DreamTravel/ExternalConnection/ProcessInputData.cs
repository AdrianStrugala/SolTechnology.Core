using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using DreamTravel.Models;
using Newtonsoft.Json.Linq;

namespace DreamTravel.ExternalConnection
{
    public class ProcessInputData : IProcessInputData
    {
        private static double FuelPrice { get; } = 1.26;
        private static double RoadVelocity { get; } = 70;
        private static double HighwayVelocity { get; } = 120;
        private static double RoadCombustion { get; } = 0.06; //per km

        private readonly ICallAPI _APICaller;


        public ProcessInputData(ICallAPI apiCaller)
        {
            _APICaller = apiCaller;
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

        public async Task<EvaluationMatrix> EvaluateCostAsync(EvaluationMatrix evaluationMatrix, int noOfCities)
        {
            await noOfCities.ForEachAsync(async i =>
            {
                await noOfCities.ForEachAsync(async j =>
                {
                    await Task.Run(() =>
                    {
                        if (i != j)
                        {
                            int iterator = j + i * noOfCities;

                            //if toll takes more time than regular -> pretend it does not exist
                            if (evaluationMatrix.TollDistances[iterator] > evaluationMatrix.FreeDistances[iterator])
                            {
                                evaluationMatrix.TollDistances[iterator] = evaluationMatrix.FreeDistances[iterator];
                                evaluationMatrix.Costs[iterator] = 0;
                            }

                            if (IsTollRoadProfitable(evaluationMatrix, iterator))
                            {
                                evaluationMatrix.OptimalDistances[iterator] = evaluationMatrix.TollDistances[iterator];
                                evaluationMatrix.OptimalCosts[iterator] = evaluationMatrix.Costs[iterator];
                            }
                            else
                            {
                                evaluationMatrix.OptimalDistances[iterator] = evaluationMatrix.FreeDistances[iterator];
                                evaluationMatrix.OptimalCosts[iterator] = 0;
                            }
                        }
                    });
                });
            });

            return evaluationMatrix;
        }

        private static bool IsTollRoadProfitable(EvaluationMatrix evaluationMatrix, int iterator)
        {
            // C_G=s×combustion×fuel price [€] = v x t x combustion x fuel 
            double gasolineCostFree =
                evaluationMatrix.FreeDistances[iterator] /
                3600.0 * RoadVelocity * RoadCombustion * FuelPrice;

            // 
            double gasolineCostToll =
                evaluationMatrix.TollDistances[iterator] /
                3600.0 * HighwayVelocity * RoadCombustion * 1.25 * FuelPrice;

            //toll goal = (cost of gasoline + cost of toll fee) * time of toll
            double cost = (gasolineCostToll + evaluationMatrix.Costs[iterator]);
            double time = (evaluationMatrix.TollDistances[iterator] / 3600.0);
            double importance = (evaluationMatrix.TollDistances[iterator] * 1.0 /
                                 evaluationMatrix.FreeDistances[iterator] * 1.0);
            var tollGoal = cost * time * importance;
            var freeGoal = gasolineCostFree * (evaluationMatrix.FreeDistances[iterator] / 3600.0);


            evaluationMatrix.Goals[iterator] = tollGoal;

            return freeGoal > tollGoal;
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
                City downloadedCity = await _APICaller.DownloadLocationOfCity(cityName);
                cities.Add(downloadedCity);
            }

            return cities;
        }

        public async Task<double> GetCostBetweenTwoCities(City origin, City destination)
        {
            var costInCents = await _APICaller.DowloadCostBetweenTwoCities(origin, destination);
            return costInCents / 100;
        }

        public async Task<int> GetDurationBetweenTwoCitiesByTollRoad(City origin, City destination)
        {
            return await _APICaller.DowloadDurationBetweenTwoCitesByTollRoad(origin, destination);
        }

        public async Task<int> GetDurationBetweenTwoCitiesByFreeRoad(City origin, City destination)
        {
            return await _APICaller.DowloadDurationBetweenTwoCitesByFreeRoad(origin, destination);
        }
    }

    public static class Extensions
    {
        public static async Task ForEachAsync<T>(this List<T> list, Func<T, Task> func)
        {
            foreach (var value in list)
            {
                await func(value);
            }
        }

        public static async Task ForEachAsync(this int iterator, Func<int, Task> func)
        {
            for (int i = 0; i < iterator; i++)
            {
                await func(i);
            }
        }
    }
}
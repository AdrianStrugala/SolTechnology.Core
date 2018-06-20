using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using DreamTravel.Models;
using Newtonsoft.Json.Linq;

namespace DreamTravel.ExternalConnection
{
    public class ProcessInputData
    {
        private static double FuelPrice { get; } = 1.26;
        private static double RoadVelocity { get; } = 70;
        private static double HighwayVelocity { get; } = 120;
        private static double RoadCombustion { get; } = 0.06; //per km

        private readonly CallAPI _APICaller;


        public ProcessInputData()
        {
            _APICaller = new CallAPI();
        }

        internal EvaluationMatrix FillMatrixWithData(List<City> listOfCities, EvaluationMatrix evaluationMatrix)
        {
            ProcessInputData processInputData = new ProcessInputData();

            Parallel.For(0, listOfCities.Count, i =>
            {
                Parallel.For(0, listOfCities.Count, j =>
                {
                    int iterator = j + i * listOfCities.Count;

                    if (i == j)
                    {
                        SetTablesValueAsMax(evaluationMatrix, iterator);
                    }

                    else
                    {
                        Parallel.Invoke(
                            () => evaluationMatrix.FreeDistances[iterator] =
                                processInputData.GetDurationBetweenTwoCitiesByFreeRoad(listOfCities[i],
                                    listOfCities[j]),
                            () => evaluationMatrix.TollDistances[iterator] =
                                processInputData.GetDurationBetweenTwoCitiesByTollRoad(listOfCities[i],
                                    listOfCities[j]),
                            () => evaluationMatrix.Costs[iterator] =
                                processInputData.GetCostBetweenTwoCities(listOfCities[i], listOfCities[j])
                        );

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
            double importance = (evaluationMatrix.TollDistances[iterator] * 1.0 / evaluationMatrix.FreeDistances[iterator] * 1.0);
            var tollGoal = cost * time * importance;
            var freeGoal = gasolineCostFree * (evaluationMatrix.FreeDistances[iterator] / 3600.0);

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

        public List<City> GetCitiesFromGoogleApi(List<string> cityNames)
        {
            List<City> cities = new List<City>();
            foreach (var cityName in cityNames)
            {
                City toAdd = new City { Name = cityName };

                JObject locationJson = _APICaller.DownloadLocationOfCity(cityName);
                toAdd.Latitude = locationJson["results"][0]["geometry"]["location"]["lat"].Value<double>();
                toAdd.Longitude = locationJson["results"][0]["geometry"]["location"]["lng"].Value<double>();

                cities.Add(toAdd);
            }

            return cities;
        }

        public double GetCostBetweenTwoCities(City origin, City destination)
        {
            var content = _APICaller.DowloadCostBetweenTwoCities(origin, destination);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(content);

            XmlNode node = doc.DocumentElement.SelectSingleNode("/response/iti/header/summaries/summary/tollCost/car");
            double tollCost = Convert.ToDouble(node.InnerText);

            XmlNode vinietaNode = doc.DocumentElement.SelectSingleNode("/response/iti/header/summaries/summary/CCZCost/car");
            double vinietaCost = Convert.ToDouble(vinietaNode.InnerText);

            double result = tollCost + vinietaCost;

            return result / 100;
        }

        public int GetDurationBetweenTwoCitiesByTollRoad(City origin, City destination)
        {
            JObject json = _APICaller.DowloadDurationBetweenTwoCitesByTollRoad(origin, destination);

            try
            {
                return json["rows"][0]["elements"][0]["duration"]["value"].Value<int>();
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public int GetDurationBetweenTwoCitiesByFreeRoad(City origin, City destination)
        {
            JObject json = _APICaller.DowloadDurationBetweenTwoCitesByFreeRoad(origin, destination);
            try
            {
                return json["rows"][0]["elements"][0]["duration"]["value"].Value<int>();
            }
            catch (Exception)
            {
                return -1;
            }
        }
    }
}
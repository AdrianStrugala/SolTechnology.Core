using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json.Linq;
using TESWebUI.Models;

namespace TESWebUI.TSPEngine
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

        internal DistanceMatrixEvaluated DownloadDataToMatrix(List<City> listOfCities, DistanceMatrixEvaluated distanceMatrix)
        {
            ProcessInputData processInputData = new ProcessInputData();

            Parallel.For(0, listOfCities.Count, i =>
            {
                Parallel.For(0, listOfCities.Count, j =>
                {

                    if (i == j)
                    {
                        distanceMatrix.Distances[j + i * listOfCities.Count] = Double.MaxValue;
                        distanceMatrix.Goals[j + i * listOfCities.Count] = Double.MaxValue;
                        distanceMatrix.Costs[j + i * listOfCities.Count] = Double.MaxValue;
                    }
                    else
                    {
                        int timeFree = -1;
                        int timeToll = -1;
                        double costToll = -1;

                        Parallel.Invoke(
                            () => timeFree =
                                processInputData.GetDurationBetweenTwoCitiesByFreeRoad(listOfCities[i],
                                    listOfCities[j]),
                            () => timeToll =
                                processInputData.GetDurationBetweenTwoCitiesByTollRoad(listOfCities[i],
                                    listOfCities[j]),
                            () => costToll =
                                processInputData.GetCostBetweenTwoCities(listOfCities[i], listOfCities[j])
                        );
                        // C_G=s×combustion×fuel price [€] = v x t x combustion x fuel 
                        double gasolineCostFree =
                            timeFree /
                            3600.0 * RoadVelocity * RoadCombustion * FuelPrice;

                        // 
                        double gasolineCostToll =
                            timeToll /
                            3600.0 * HighwayVelocity * RoadCombustion * 1.25 * FuelPrice;


                        //toll goal = (cost of gasoline + cost of toll fee) * time of toll
                        double cost = (gasolineCostToll + costToll);
                        double time = (timeToll / 3600.0);
                        double importance = (timeToll * 1.0 / timeFree * 1.0);
                        double tollGoal = cost * time * importance;

                        var freeGoal =
                            gasolineCostFree * (timeFree / 3600.0);

                        if (freeGoal < tollGoal)
                        {
                            distanceMatrix.Distances[j + i * listOfCities.Count] = timeFree;
                            distanceMatrix.Goals[j + i * listOfCities.Count] = freeGoal;
                            distanceMatrix.Costs[j + i * listOfCities.Count] = 0;
                        }
                        else
                        {
                            distanceMatrix.Distances[j + i * listOfCities.Count] = timeToll;
                            distanceMatrix.Goals[j + i * listOfCities.Count] = tollGoal;
                            distanceMatrix.Costs[j + i * listOfCities.Count] = costToll;
                        }
                    }
                });
            });

            return distanceMatrix;
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
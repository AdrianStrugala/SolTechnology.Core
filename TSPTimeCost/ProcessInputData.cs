using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TSPTimeCost.Models;
using TSPTimeCost.Singletons;

/**********************************************************
 * Process input from list of cities                      *
 * to matrixes of distanses and cost                      *
 * *******************************************************/

namespace TSPTimeCost
{
    class ProcessInputData
    {
        private static  ViewModel _viewModel;

        private static double FuelPrice { get; } = 1.26;
        private static double RoadVelocity { get; } = 70;
        private static double HighwayVelocity { get; } = 120;
        private static double RoadCombustion { get; } = 0.06; //per km

        //DECIDE HOW TO TAKE INPUT
        //        public List<Road> ReadInputFile() {
        //            List<Road> result = new List<Road>();
        //
        //            using (var mappedFile1 = MemoryMappedFile.CreateFromFile("C:/Users/Adrian/Desktop/input.txt")) {
        //                using (Stream mmStream = mappedFile1.CreateViewStream()) {
        //                    using (StreamReader sr = new StreamReader(mmStream, Encoding.ASCII)) {
        //                        while (!sr.EndOfStream) {
        //
        //                            Road toAdd = new Road();
        //                            var line = sr.ReadLine();
        //                            var lineWords = line.Split(' ');
        //
        //                            toAdd.Beginning = lineWords[0];
        //                            toAdd.Ending = lineWords[1];
        //                            toAdd.Time = Convert.ToDouble(lineWords[2]);
        //                            toAdd.Cost = Convert.ToDouble(lineWords[3]);
        //
        //                            result.Add(toAdd);
        //                        }
        //                    }
        //                }
        //            }
        //
        //            return result;
        //        }

        private static List<string> ReadCities()
        {
            _viewModel = new ViewModel();
            string[] cities = _viewModel.Cities.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            );

            // List<string> cities = new List<string> { "Como, Italy", "Verona", "Florence", "Pisa", "Turin", "Milan", "Genoa", "Bergamo" };
            //  List<string> cities = new List<string> { "Como", "Verona", "Florence", "Turin", "Milan"};
            // List<string> _cities = new List<string> { "Wroclaw", "Lodz", "Warszawa", "Krakow", "Poznan", "Gdansk", "Lublin", "Bialystok" };

            return cities.Where(x => !string.IsNullOrEmpty(x)).ToList();
        }

        public static void CalculateCostMatrix()
        {
            List<City> cities = Cities.Instance.ListOfCities;
            CostMatrix.Instance.Value = new double[cities.Count * cities.Count];

            //            for (int i = 0; i < cities.Count; i++)
            //            {
            //                for (int j = 0; j < cities.Count; j++)
            //                {
            //                    if (i == j)
            //                    {
            //                        CostMatrix.Instance.Distances[j + i * cities.Count] = double.MaxValue;
            //                    }
            //                    else
            //                    {
            //                        CostMatrix.Instance.Distances[j + i * cities.Count] =
            //                            GetCostBetweenTwoCities(cities[i], cities[j]);
            //                    }
            //                }
            //            }

            using (var mappedFile1 = MemoryMappedFile.CreateFromFile(@"..\..\CostMatrix.txt"))
            {
                using (Stream mmStream = mappedFile1.CreateViewStream())
                {
                    using (StreamReader sr = new StreamReader(mmStream, Encoding.ASCII))
                    {
                        while (!sr.EndOfStream)
                        {


                            for (int i = 0; i < 8; i++)
                            {
                                var line = sr.ReadLine();
                                var lineWords = line.Split(' ');

                                for (int j = 0; j < 8; j++)
                                {
                                    CostMatrix.Instance.Value[i * 8 + j] = Convert.ToDouble(lineWords[j]);
                                }
                            }

                        }
                    }
                }
            }
            //
            //            CostMatrix.Instance.Distances = {
            //                double.MaxValue; 11.9, 37.6, 31.4, 23.2, 3.9 15.1 4.5
            //            15.3 1.79769313486232E+308 34.1 20.3 27.8 11.2 21.3 7.5
            //            28 16.3 1.79769313486232E+308 4.8 37.4 22.3 21.5 23.1
            //            31.9 20.3 4.8 1.79769313486232E+308 31.3 26.2 15.4 33.2
            //            23.7 27.8 37.4 31.3 1.79769313486232E+308 16.9 15 20.3
            //            4.4 11.2 32.4 26.2 16.9 1.79769313486232E+308 9.9 3.8
            //            15.6 21.3 21.5 15.4 15 9.9 1.79769313486232E+308 13.7
            //            7.8 7.5 39.3 33.2 20.3 3.8 13.7 1.79769313486232E+308};

            //            //save to file (in case of licence expirence)
            //            using (StreamWriter file =
            //                new StreamWriter(@"..\..\CostMatrix.txt"))
            //            {
            //                for (int i = 0; i < cities.Count; i++)
            //                {
            //                    for (int j = 0; j < cities.Count; j++)
            //                    {
            //                        file.Write(CostMatrix.Instance.Distances[j + i * cities.Count] + " ");
            //
            //                    }
            //                    file.Write("\n");
            //                }
            //
            //            }
        }

        public static void CalculateDistanceMatrixForFreeRoads()
        {
            List<City> cities = Cities.Instance.ListOfCities;
            DistanceMatrixForFreeRoads.Instance.Distances = new double[cities.Count * cities.Count];

            for (int i = 0; i < cities.Count; i++)
            {
                for (int j = 0; j < cities.Count; j++)
                {
                    if (i == j)
                    {
                        DistanceMatrixForFreeRoads.Instance.Distances[j + i * cities.Count] = double.MaxValue;
                    }
                    else
                    {
                        DistanceMatrixForFreeRoads.Instance.Distances[j + i * cities.Count] =
                            GetDurationBetweenTwoCitiesByFreeRoad(cities[i].Latitude, cities[i].Longitude, cities[j].Latitude, cities[j].Longitude);
                    }
                }
            }
        }

        public static void CalculateDistanceMatrixForTollRoads()
        {
            List<City> cities = Cities.Instance.ListOfCities;
            DistanceMatrixForTollRoads.Instance.Distances = new double[cities.Count * cities.Count];

            for (int i = 0; i < cities.Count; i++)
            {
                for (int j = 0; j < cities.Count; j++)
                {
                    if (i == j)
                    {
                        DistanceMatrixForTollRoads.Instance.Distances[j + i * cities.Count] = Double.MaxValue;
                    }
                    else
                    {
                        DistanceMatrixForTollRoads.Instance.Distances[j + i * cities.Count] =
                            GetDurationBetweenTwoCitiesByTollRoad(cities[i].Latitude, cities[i].Longitude, cities[j].Latitude, cities[j].Longitude);
                    }
                }
            }
        }

        //        private static double GetCostBetweenTwoCities(City origin, City destination)
        //        {
        //
        //            string url =
        //                $"http://apir.viamichelin.com/apir/1/route.xml/fra?steps=1:e:{origin.Longitude}:{origin.Latitude};1:e:{destination.Longitude}:{destination.Latitude}&authkey=RESTGP20171016131341697440740272";
        //
        //            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        //
        //            request.Method = "GET";
        //            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36";
        //            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
        //
        //            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        //
        //            string content;
        //            using (Stream stream = response.GetResponseStream())
        //            {
        //                using (StreamReader sr = new StreamReader(stream))
        //                {
        //                    content = sr.ReadToEnd();
        //                }
        //            }
        //
        //            XmlDocument doc = new XmlDocument();
        //
        //            doc.LoadXml(content);
        //
        //            XmlNode node = doc.DocumentElement.SelectSingleNode("/response/iti/header/summaries/summary/tollCost/car");
        //            double result = Convert.ToDouble(node.InnerText);
        //
        //            return result / 100;
        //        }

        private static int GetDurationBetweenTwoCitiesByTollRoad(double originLan, double originLon, double destinationLan, double destinationLon)
        {

            string url =
                $"https://maps.googleapis.com/maps/api/distancematrix/json?units=imperial&origins={originLan},{originLon}&destinations={destinationLan},{destinationLon}&key=AIzaSyCdHbtbmF8Y2nfesiu0KUUJagdG7_oui1k";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            request.Method = "GET";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36";
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            string content;
            using (Stream stream = response.GetResponseStream())
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    content = sr.ReadToEnd();
                }
            }

            var json = JObject.Parse(content);

            try
            {
                return json["rows"][0]["elements"][0]["duration"]["value"].Value<int>();
            }
            catch (Exception ex)
            {
            }
            return 0;
        }

        private static int GetDurationBetweenTwoCitiesByFreeRoad(double originLan, double originLon, double destinationLan, double destinationLon)
        {

            string url =
                $"https://maps.googleapis.com/maps/api/distancematrix/json?units=imperial&origins={originLan},{originLon}&destinations={destinationLan},{destinationLon}&avoid=tolls&key=AIzaSyCdHbtbmF8Y2nfesiu0KUUJagdG7_oui1k";
            //  string url = $"https://maps.googleapis.com/maps/api/distancematrix/json?units=imperial&origins={originLan},{originLon}&destinations={destinationLan},{destinationLon}&mode=walking&avoid=tolls&key=AIzaSyCdHbtbmF8Y2nfesiu0KUUJagdG7_oui1k";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            request.Method = "GET";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36";
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            string content;
            using (Stream stream = response.GetResponseStream())
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    content = sr.ReadToEnd();
                }
            }

            var json = JObject.Parse(content);

            return json["rows"][0]["elements"][0]["duration"]["value"].Value<int>();

        }

        public static void InitializeSingletons()
        {
            Cities.Instance.ListOfCities = new List<City>();
            Cities.Instance.ListOfCities = GetCitiesFromGoogleApi();
            int noOfCities = Cities.Instance.ListOfCities.Count;

            BestPath.Instance.Order = new int[noOfCities];
            BestPath.Instance.Goal = new double[noOfCities];
            DistanceMatrixForTollRoads.Instance.Distances = new double[noOfCities * noOfCities];
            DistanceMatrixForFreeRoads.Instance.Distances = new double[noOfCities * noOfCities];
            DistanceMatrixEvaluated.Instance.Distances = new double[noOfCities * noOfCities];
            DistanceMatrixEvaluated.Instance.Goals = new double[noOfCities * noOfCities];
            DistanceMatrixForTollRoads.Instance.Goals = new double[noOfCities * noOfCities];
            DistanceMatrixForFreeRoads.Instance.Goals = new double[noOfCities * noOfCities];


            //Fist bestPath is just cities in input order
            for (int i = 0; i < noOfCities; i++)
            {
                BestPath.Instance.Order[i] = i;
            }

            for (int i = 0; i < noOfCities; i++)
            {
                BestPath.Instance.Goal[i] = 0;
            }

            BestPath.Instance.DistancesInOrder = new double[noOfCities - 1];

        }


        public static List<City> GetCitiesFromGoogleApi()
        {
            List<City> cities = new List<City>();
            List<string> cityNames = ReadCities();

            foreach (var cityName in cityNames)
            {
                City toAdd = new City { Name = cityName };

                JObject locationJson = GetLocationJson(cityName);
                toAdd.Latitude = locationJson["results"][0]["geometry"]["location"]["lat"].Value<double>();
                toAdd.Longitude = locationJson["results"][0]["geometry"]["location"]["lng"].Value<double>();

                cities.Add(toAdd);
            }

            return cities;
        }

        private static JObject GetLocationJson(string cityName)
        {
            string url =
                $"https://maps.googleapis.com/maps/api/geocode/json?address={cityName}&key=AIzaSyBgCjCJuGQsXlAz6BUXPIL2_RSxgXUaCcM";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.AutomaticDecompression = DecompressionMethods.GZip;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                var serializer = new JsonSerializer();

                using (var jsonTextReader = new JsonTextReader(reader))
                {
                    JObject json = (JObject)serializer.Deserialize(jsonTextReader);
                    return json;
                }
            }

        }

        public static void CalculateDistanceMatrixEvaluated()
        {
            List<City> cities = Cities.Instance.ListOfCities;

            DistanceMatrixEvaluated.Instance.Distances = new double[cities.Count * cities.Count];

            for (int i = 0; i < cities.Count; i++)
            {
                for (int j = 0; j < cities.Count; j++)
                {
                    // C_G=s×combustion×fuel price [€]
                    double gasolineCostFree =
                        DistanceMatrixForFreeRoads.Instance.Distances[j + i * cities.Count] /
                        3600 * RoadVelocity * RoadCombustion * FuelPrice;

                    double gasolineCostToll =
                        DistanceMatrixForTollRoads.Instance.Distances[j + i * cities.Count] /
                        3600 * HighwayVelocity * RoadCombustion * 1.25 * FuelPrice;

                    var tollGoal = DistanceMatrixForTollRoads.Instance.Goals[
                            j + i * cities.Count] = gasolineCostToll *
                                   (DistanceMatrixForTollRoads.Instance.Distances[
                                        j + i * cities.Count] / 3600);

                    var freeGoal = DistanceMatrixForFreeRoads.Instance.Goals[
                             j + i * cities.Count] = gasolineCostFree *
                                                     (DistanceMatrixForFreeRoads.Instance.Distances[
                                                          j + i * cities.Count] / 3600);

                    if (i == j)
                    {
                        DistanceMatrixEvaluated.Instance.Distances[j + i * cities.Count] = double.MaxValue;
                    }
                    else if (freeGoal < tollGoal)
                    {
                        DistanceMatrixEvaluated.Instance.Distances[j + i * cities.Count] =
                            DistanceMatrixForFreeRoads.Instance.Distances[j + i * cities.Count];
                        DistanceMatrixEvaluated.Instance.Goals[j + i * cities.Count] = freeGoal;
                    }
                    else
                    {
                        DistanceMatrixEvaluated.Instance.Distances[j + i * cities.Count] =
                            DistanceMatrixForTollRoads.Instance.Distances[j + i * cities.Count];
                        DistanceMatrixEvaluated.Instance.Goals[j + i * cities.Count] = tollGoal;
                    }
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TSPTimeCost.Models;

/**********************************************************
 * Process input from list of cities                      *
 * to matrixes of distanses and cost                      *
 * *******************************************************/

namespace TSPTimeCost
{
    class ProcessInputData
    {


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
           // List<string> cities = new List<string> { "Como", "Verona", "Florence", "Pisa", "Turin", "Milan", "Genoa", "Bergamo" };
            List<string> cities = new List<string> { "Como", "Verona", "Florence", "Turin", "Milan"};
            // List<string> _cities = new List<string> { "Wroclaw", "Lodz", "Warszawa", "Krakow", "Poznan", "Gdansk", "Lublin", "Bialystok" };

            return cities;
        }

        public void CalculateCostMatrix(List<City> cities)
        {

            CostMatrix.Instance.Value = new double[cities.Count * cities.Count];

            for (int i = 0; i < cities.Count; i++)
            {
                for (int j = 0; j < cities.Count; j++)
                {
                    if (i == j)
                    {
                        CostMatrix.Instance.Value[j + i * cities.Count] = double.MaxValue;
                    }
                    else
                    {
                        CostMatrix.Instance.Value[j + i * cities.Count] =
                            GetCostBetweenTwoCities(cities[i], cities[j]);
                    }
                }
            }

//            //save to file (in case of licence expirence)
//            using (StreamWriter file =
//                new StreamWriter(@"..\..\CostMatrix.txt"))
//            {
//                for (int i = 0; i < cities.Count; i++)
//                {
//                    for (int j = 0; j < cities.Count; j++)
//                    {
//                        file.Write(CostMatrix.Instance.Value[j + i * cities.Count] + " ");
//
//                    }
//                    file.Write("\n");
//                }
//
//            }
        }

        public void CalculateDistanceMatrixForFreeRoads(List<City> cities)
        {

            DistanceMatrixForFreeRoads.Instance.Value = new double[cities.Count * cities.Count];

            for (int i = 0; i < cities.Count; i++)
            {
                for (int j = 0; j < cities.Count; j++)
                {
                    if (i == j)
                    {
                        DistanceMatrixForFreeRoads.Instance.Value[j + i * cities.Count] = double.MaxValue;
                    }
                    else
                    {
                        DistanceMatrixForFreeRoads.Instance.Value[j + i * cities.Count] =
                            GetDurationBetweenTwoCitiesByFreeRoad(cities[i].Latitude, cities[i].Longitude, cities[j].Latitude, cities[j].Longitude);
                    }
                }
            }
        }

        public static void CalculateDistanceMatrixForTollRoads(List<City> cities)
        {

            DistanceMatrixForTollRoads.Instance.Value = new double[cities.Count * cities.Count];

            for (int i = 0; i < cities.Count; i++)
            {
                for (int j = 0; j < cities.Count; j++)
                {
                    if (i == j)
                    {
                        DistanceMatrixForTollRoads.Instance.Value[j + i * cities.Count] = Double.MaxValue;
                    }
                    else
                    {
                        DistanceMatrixForTollRoads.Instance.Value[j + i * cities.Count] =
                            GetDurationBetweenTwoCitiesByTollRoad(cities[i].Latitude, cities[i].Longitude, cities[j].Latitude, cities[j].Longitude);
                    }
                }
            }
        }

        private static double GetCostBetweenTwoCities(City origin, City destination)
        {

            string url =
                $"http://apir.viamichelin.com/apir/1/route.xml/fra?steps=1:e:{origin.Longitude}:{origin.Latitude};1:e:{destination.Longitude}:{destination.Latitude}&authkey=RESTGP20171016131341697440740272";

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

            XmlDocument doc = new XmlDocument();

            doc.LoadXml(content);

            XmlNode node = doc.DocumentElement.SelectSingleNode("/response/iti/header/summaries/summary/tollCost/car");
            double result = Convert.ToDouble(node.InnerText);

            return result / 100;
        }

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


            return json["rows"][0]["elements"][0]["duration"]["value"].Value<int>();

        }

        private int GetDurationBetweenTwoCitiesByFreeRoad(double originLan, double originLon, double destinationLan, double destinationLon)
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

        public static void InitializeSingletons(int noOfCities)
        {
            BestPath.Instance.Order = new int[noOfCities];
            DistanceMatrixForTollRoads.Instance.Value = new double[noOfCities * noOfCities];
            DistanceMatrixForFreeRoads.Instance.Value = new double[noOfCities * noOfCities];

            //Fist bestPath is just cities in input order
            for (int i = 0; i < noOfCities; i++)
            {
                BestPath.Instance.Order[i] = i;
            }

            BestPath.Instance.DistancesInOrder = new double[noOfCities - 1];

        }


        public List<City> GetCitiesFromGoogleApi()
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
    }
}

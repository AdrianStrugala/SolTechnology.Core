using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Parallel_Ants;

/**********************************************************
 * Process input from list of cities with coordinates     *
 * to matrix of distanses                                 *
 * *******************************************************/

namespace TSPTimeCost
{
    class ProcessInputData
    {

        public List<Road> ReadInputFile()
        {
            List<Road> result = new List<Road>();

            using (var mappedFile1 = MemoryMappedFile.CreateFromFile("C:/Users/Adrian/Desktop/input.txt"))
            {
                using (Stream mmStream = mappedFile1.CreateViewStream())
                {
                    using (StreamReader sr = new StreamReader(mmStream, Encoding.ASCII))
                    {
                        while (!sr.EndOfStream)
                        {

                            Road toAdd = new Road();
                            var line = sr.ReadLine();
                            var lineWords = line.Split(' ');

                            toAdd.Beginning = lineWords[0];
                            toAdd.Ending = lineWords[1];
                            toAdd.Time = Convert.ToDouble(lineWords[2]);
                            toAdd.Cost = Convert.ToDouble(lineWords[3]);

                            result.Add(toAdd);
                        }
                    }
                }
            }

            return result;
        }

        public List<string> ReadCities()
        {
            List<string> cities = new List<string>();
            cities.Add("Modena");
            cities.Add("Verona");
            cities.Add("Bergamo");

            return cities;
        }


        /*        public void CalculateDistanceMatrix(List<Road> cities) {

                    DistanceMatrix.Instance.value = new double[cities.Count * cities.Count];

                    for (int i = 0; i < cities.Count; i++) {
                        for (int j = 0; j < cities.Count; j++) {
                            if (i == j) {
                                DistanceMatrix.Instance.value[j + i * cities.Count] = Double.MaxValue;
                            }
                            else {
                                DistanceMatrix.Instance.value[j + i * cities.Count] =
                                    new Coordinates().distance((double)cities[i].Y, (double)cities[i].X, (double)cities[j].Y, (double)cities[j].X, 'K');
                            }
                        }
                    }
                }*/

        public void InitializeSingletons(int noOfCities)
        {
            BestPath.Instance.order = new int[noOfCities];
            DistanceMatrix.Instance.value = new double[noOfCities * noOfCities];

            //Fist bestPath is just cities in input order
            for (int i = 0; i < noOfCities; i++)
            {
                BestPath.Instance.order[i] = i;
            }
        }


        public List<City> GetCitiesFromGoogleApi()
        {
            List<City> cities = new List<City>();
            List<string> cityNames = ReadCities();

            foreach (var cityName in cityNames)
            {
                City toAdd = new City();
                toAdd.Name = cityName;

                JObject locationJson = GetLocationJson(cityName);
                toAdd.Latitude = locationJson["results"][0]["geometry"]["location"]["lat"].Value<double>();
                toAdd.Longitude = locationJson["results"][0]["geometry"]["location"]["lng"].Value<double>();

                cities.Add(toAdd);
            }

            return cities;
        }

        JObject GetLocationJson(string cityName)
        {
            string url =
            (string.Format(
                "https://maps.googleapis.com/maps/api/geocode/json?address={0}&key=AIzaSyBgCjCJuGQsXlAz6BUXPIL2_RSxgXUaCcM",
                cityName));

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

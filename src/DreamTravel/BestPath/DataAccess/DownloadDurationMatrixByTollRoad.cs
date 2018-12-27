using DreamTravel.BestPath.Interfaces;
using DreamTravel.SharedModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;

namespace DreamTravel.BestPath.DataAccess
{
    public class DownloadDurationMatrixByTollRoad : IDownloadDurationMatrixByTollRoad
    {
        private readonly HttpClient _httpClient;

        public DownloadDurationMatrixByTollRoad()
        {
            if (_httpClient == null)
            {
                _httpClient = new HttpClient();
            }

        }

        public double[] Execute(List<City> listOfCities)
        {
            double[] result = new double[listOfCities.Count * listOfCities.Count];

            StringBuilder coordinates = new StringBuilder();
            foreach (City city in listOfCities)
            {
                coordinates.AppendFormat($"{city.Latitude},{city.Longitude}|");
            }

            try
            {
                for (int i = 0; i < listOfCities.Count; i++)
                {
                    string url =
                        $"https://maps.googleapis.com/maps/api/distancematrix/json?units=imperial&origins={listOfCities[i].Latitude},{listOfCities[i].Longitude}&destinations={coordinates}&key=AIzaSyBgCjCJuGQsXlAz6BUXPIL2_RSxgXUaCcM";

                    HttpResponseMessage getAsync = _httpClient.GetAsync(url).Result;

                    using (Stream stream = getAsync.Content.ReadAsStreamAsync().Result ??
                                           throw new ArgumentNullException(
                                               $"Exception on [{MethodBase.GetCurrentMethod().Name}]"))
                    {
                        using (var jsonTextReader = new JsonTextReader(new StreamReader(stream)))
                        {
                            JObject json = (JObject)new JsonSerializer().Deserialize(jsonTextReader);

                            for (int j = 0; j < listOfCities.Count; j++)
                            {
                                result[j + i * listOfCities.Count] = json["rows"][0]["elements"][j]["duration"]["value"].Value<int>();
                            }
                        }
                    }
                }
                return result;
            }
            catch (Exception e)
            {
                throw new InvalidDataException(e.Message);
            }
        }

        public double[] ExecuteV2(City origin, List<City> destinations)
        {
            double[] result = new double[destinations.Count];

            StringBuilder coordinates = new StringBuilder();
            foreach (City city in destinations)
            {
                coordinates.AppendFormat($"{city.Latitude},{city.Longitude}|");
            }

            try
            {
                string url =
                    $"https://maps.googleapis.com/maps/api/distancematrix/json?units=imperial&origins={origin.Latitude},{origin.Longitude}&destinations={coordinates}&key=AIzaSyBgCjCJuGQsXlAz6BUXPIL2_RSxgXUaCcM";

                HttpResponseMessage getAsync = _httpClient.GetAsync(url).Result;

                using (Stream stream = getAsync.Content.ReadAsStreamAsync().Result ??
                                       throw new ArgumentNullException(
                                           $"Exception on [{MethodBase.GetCurrentMethod().Name}]"))
                {
                    using (var jsonTextReader = new JsonTextReader(new StreamReader(stream)))
                    {
                        JObject json = (JObject)new JsonSerializer().Deserialize(jsonTextReader);

                        for (int j = 0; j < destinations.Count; j++)
                        {
                            result[j] = json["rows"][0]["elements"][j]["duration"]["value"].Value<int>();
                        }
                    }
                }

                return result;
            }
            catch (Exception e)
            {
                throw new InvalidDataException(e.Message);
            }
        }
    }
}

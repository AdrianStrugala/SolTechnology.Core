using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using DreamTravel.BestPath.Interfaces;
using DreamTravel.SharedModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DreamTravel.BestPath.DataAccess
{
    public class DownloadDurationMatrixByFreeRoad : IDownloadDurationMatrixByFreeRoad
    {
        private readonly HttpClient _httpClient;

        public DownloadDurationMatrixByFreeRoad()
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
                        $"https://maps.googleapis.com/maps/api/distancematrix/json?units=imperial&origins={listOfCities[i].Latitude},{listOfCities[i].Longitude}&destinations={coordinates}&avoid=tolls&key=AIzaSyCdHbtbmF8Y2nfesiu0KUUJagdG7_oui1k";

                    HttpResponseMessage getAsync = _httpClient.GetAsync(url).Result;

                    using (Stream stream = getAsync.Content.ReadAsStreamAsync().Result ??
                                           throw new ArgumentNullException(
                                               $"Execption on [{MethodBase.GetCurrentMethod().Name}]"))
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
    }
}

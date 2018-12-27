using DreamTravel.BestPath.Interfaces;
using DreamTravel.SharedModels;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;

namespace DreamTravel.BestPath.DataAccess
{
    using System.Threading.Tasks;

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

        public async Task<double[]> Execute(List<City> listOfCities)
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
                        $"https://maps.googleapis.com/maps/api/distancematrix/json?units=imperial&origins={listOfCities[i].Latitude},{listOfCities[i].Longitude}&destinations={coordinates}&avoid=tolls&key=AIzaSyBgCjCJuGQsXlAz6BUXPIL2_RSxgXUaCcM";

                    string response = await _httpClient.GetStringAsync(url);
                    JObject json = JObject.Parse(response);

                    for (int j = 0; j < listOfCities.Count; j++)
                    {
                        result[j + i * listOfCities.Count] = json["rows"][0]["elements"][j]["duration"]["value"].Value<int>();
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

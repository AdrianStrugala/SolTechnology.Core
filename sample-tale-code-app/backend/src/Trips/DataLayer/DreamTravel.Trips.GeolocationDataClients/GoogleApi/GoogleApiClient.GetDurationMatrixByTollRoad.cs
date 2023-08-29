using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DreamTravel.GeolocationData.Configuration;
using DreamTravel.Trips.Domain.Cities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace DreamTravel.GeolocationData.GoogleApi
{
    public partial class GoogleApiClient : IGoogleApiClient
    {
        public async Task<double[]> GetDurationMatrixByTollRoad(List<City> listOfCities)
        {
            double[] result = new double[listOfCities.Count * listOfCities.Count];

            StringBuilder coordinates = new StringBuilder();
            foreach (City city in listOfCities)
            {
                coordinates.AppendFormat($"{city.Latitude},{city.Longitude}|");
            }

            for (int i = 0; i < listOfCities.Count; i++)
            {
                try
                {
                    string url =
                        $"https://maps.googleapis.com/maps/api/distancematrix/json?units=imperial&origins={listOfCities[i].Latitude},{listOfCities[i].Longitude}&destinations={coordinates}&key={GeolocationDataConfiguration.ApiKey}";

                    var response = await _httpClient.GetStringAsync(url);
                    JObject json = JObject.Parse(response);

                    for (int j = 0; j < listOfCities.Count; j++)
                    {
                        if (i == j)
                        {
                            result[j + i * listOfCities.Count] = double.MaxValue;
                        }
                        else
                        {
                            result[j + i * listOfCities.Count] =
                                json["rows"][0]["elements"][j]["duration"]["value"].Value<int>();
                        }
                    }
                }

                catch (Exception)
                {
                    _logger.LogError($"Cannot get data about distance when [{listOfCities[i].Name}] is the origin");
                    throw new InvalidDataException(
                        $"Cannot get data about distance when [{listOfCities[i].Name}] is the origin");
                }
            }

            return result;
        }
    }
}
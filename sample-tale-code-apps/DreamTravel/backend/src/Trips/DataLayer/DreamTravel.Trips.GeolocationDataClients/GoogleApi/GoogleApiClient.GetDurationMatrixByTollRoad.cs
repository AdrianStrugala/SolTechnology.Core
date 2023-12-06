﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
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

            try
            {
                var request = await _httpClient
                    .CreateRequest($"maps/api/distancematrix/json?units=imperial&origins={coordinates}&destinations={coordinates}&key={_options.Key}")
                    .GetAsync();

                var response = await request.Content.ReadAsStringAsync();
                JObject json = JObject.Parse(response);

                for (int i = 0; i < listOfCities.Count; i++)
                {
                    for (int j = 0; j < listOfCities.Count; j++)
                    {
                        if (i == j)
                        {
                            result[j + i * listOfCities.Count] = double.MaxValue;
                        }
                        else
                        {
                            result[j + i * listOfCities.Count] =
                                json["rows"][i]["elements"][j]["duration"]["value"].Value<int>();
                        }
                    }
                }
            }
            catch (Exception)
            {
                _logger.LogError($"Cannot get Toll Distance Matrix data");
                throw new InvalidDataException(
                    "Cannot get Toll Distance Matrix data");
            }

            return result;
        }
    }
}
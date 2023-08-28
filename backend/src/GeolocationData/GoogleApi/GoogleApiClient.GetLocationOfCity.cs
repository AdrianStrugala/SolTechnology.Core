using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using DreamTravel.Domain.Cities;
using DreamTravel.GeolocationData.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace DreamTravel.GeolocationData.GoogleApi
{
    public partial class GoogleApiClient : IGoogleApiClient
    {
        private readonly ILogger<GoogleApiClient> _logger;
        private readonly HttpClient _httpClient;

        public GoogleApiClient(ILogger<GoogleApiClient> logger)
        {
            _logger = logger;
            if (_httpClient == null)
            {
                _httpClient = new HttpClient();
            }
        }

        public async Task<City> GetLocationOfCity(string cityName)
        {
            try
            {
                string url =
                    $"https://maps.googleapis.com/maps/api/geocode/json?address={cityName}&key={GeolocationDataConfiguration.ApiKey}";

                City toAdd = new City { Name = cityName };

                string response = await _httpClient.GetStringAsync(url);
                JObject json = JObject.Parse(response);

                toAdd.Latitude = json["results"][0]["geometry"]["location"]["lat"].Value<double>();
                toAdd.Longitude = json["results"][0]["geometry"]["location"]["lng"].Value<double>();
                return toAdd;
            }

            catch (Exception)
            {
                throw new InvalidDataException($"Cannot find city [{cityName}]");
            }
        }
    }
}

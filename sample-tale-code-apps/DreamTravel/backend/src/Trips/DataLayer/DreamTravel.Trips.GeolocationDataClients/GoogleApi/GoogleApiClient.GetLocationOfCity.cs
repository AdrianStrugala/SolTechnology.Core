using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using DreamTravel.Trips.Domain.Cities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace DreamTravel.GeolocationData.GoogleApi
{
    public partial class GoogleApiClient : IGoogleApiClient
    {
        private readonly GoogleApiOptions _options;
        private readonly ILogger<GoogleApiClient> _logger;
        private readonly HttpClient _httpClient;

        public GoogleApiClient(IOptions<GoogleApiOptions> options, HttpClient httpClient, ILogger<GoogleApiClient> logger)
        {
            _options = options.Value;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<City> GetLocationOfCity(string cityName)
        {
            try
            {
                City result = new City { Name = cityName };

                var request = await _httpClient
                    .CreateRequest($"maps/api/geocode/json?address={cityName}&key={_options.Key}")
                    .GetAsync();

                var response = await request.Content.ReadAsStringAsync();
                // string url =
                //     $"https://maps.googleapis.com/maps/api/geocode/json?address={cityName}&key={_options.Key}";

                // string response = await _httpClient.GetStringAsync(url);
                JObject json = JObject.Parse(response);

                result.Latitude = json["results"][0]["geometry"]["location"]["lat"].Value<double>();
                result.Longitude = json["results"][0]["geometry"]["location"]["lng"].Value<double>();
                return result;
            }

            catch (Exception)
            {
                throw new InvalidDataException($"Cannot find city [{cityName}]");
            }
        }
    }
}

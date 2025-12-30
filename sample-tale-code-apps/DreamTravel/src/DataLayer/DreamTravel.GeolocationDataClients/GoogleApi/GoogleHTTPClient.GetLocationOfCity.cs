using DreamTravel.Domain.Cities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace DreamTravel.GeolocationDataClients.GoogleApi
{
    public partial class GoogleHTTPClient(
        IOptions<GoogleHTTPOptions> options,
        HttpClient httpClient,
        ILogger<GoogleHTTPClient> logger)
        : IGoogleHTTPClient
    {
        private readonly GoogleHTTPOptions _options = options.Value;

        public async Task<City> GetLocationOfCity(string cityName)
        {
            try
            {
                var request = await httpClient
                    .CreateRequest($"maps/api/geocode/json?address={cityName}&key={_options.Key}")
                    .GetAsync();

                var response = await request.Content.ReadAsStringAsync();
                JObject json = JObject.Parse(response);

                // Check if we have results
                var results = json["results"];
                if (results == null || !results.Any())
                {
                    throw new InvalidDataException($"No results found for city [{cityName}]");
                }

                var firstResult = results[0];
                
                // Extract coordinates
                var location = firstResult["geometry"]?["location"];
                if (location == null)
                {
                    throw new InvalidDataException($"No location data found for city [{cityName}]");
                }

                // Extract country from address_components
                var country = ExtractCountry(firstResult["address_components"]);

                var result = new City
                {
                    Name = cityName,
                    Latitude = location["lat"].Value<double>(),
                    Longitude = location["lng"].Value<double>(),
                    Country = country
                };

                return result;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Cannot find city [{cityName}]");
                throw new InvalidDataException($"Cannot find city [{cityName}]", e);
            }
        }

        private string ExtractCountry(JToken? addressComponents)
        {
            if (addressComponents == null)
            {
                return "Unknown";
            }

            // Find the component with type "country"
            foreach (var component in addressComponents)
            {
                var types = component["types"];
                if (types != null && types.Any(t => t.Value<string>() == "country"))
                {
                    // Return long_name (e.g., "United States") or short_name (e.g., "US")
                    return component["long_name"]?.Value<string>() ?? "Unknown";
                }
            }

            logger.LogWarning("Country not found in address components");
            return "Unknown";
        }
    }
}
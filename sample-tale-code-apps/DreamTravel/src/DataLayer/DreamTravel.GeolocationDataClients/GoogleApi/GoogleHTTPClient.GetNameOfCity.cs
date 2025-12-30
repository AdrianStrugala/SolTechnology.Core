using DreamTravel.Domain.Cities;
using Newtonsoft.Json.Linq;

namespace DreamTravel.GeolocationDataClients.GoogleApi
{
    public partial class GoogleHTTPClient : IGoogleHTTPClient
    {
        public async Task<City> GetNameOfCity(City city)
        {
            string url = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={city.Latitude},{city.Longitude}&sensor=false&key={_options.Key}";

            try
            {
                string response = await httpClient.GetStringAsync(url);
                JObject json = JObject.Parse(response);

                var status = json["status"]?.Value<string>();
                if (status != "OK")
                {
                    var errorMessage = json["error_message"]?.Value<string>() ?? "Unknown error";
                    throw new InvalidDataException(
                        $"Google Maps API returned status '{status}': {errorMessage}. " +
                        $"URL: {url.Replace(_options.Key, "<REDACTED>")}");
                }

                var results = json["results"];
                if (results == null || !results.Any())
                {
                    throw new InvalidDataException(
                        $"No results found for coordinates: Lat: {city.Latitude}, Lng: {city.Longitude}");
                }

                for (int i = 0; i < results[0]["address_components"].Count(); i++)
                {
                    var component = results[0]["address_components"][i];
                    var types = component["types"].Select(t => t.Value<string>()).ToList();

                    if (types.Contains("locality") || types.Contains("postal_town"))
                    {
                        city.Name = component["long_name"].Value<string>();
                    }

                    if (types.Contains("country"))
                    {
                        city.Country = component["long_name"].Value<string>();
                    }
                }

                if (city.Name == null || city.Name.Equals(string.Empty))
                {
                    city.Name = results[0]["formatted_address"].Value<string>();
                }

                return city;
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidDataException(
                    $"Failed to call Google Maps API for coordinates: Lat: {city.Latitude}, Lng: {city.Longitude}. " +
                    $"HTTP error: {ex.Message}", ex);
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException(
                    $"Unexpected error while getting city name for coordinates: Lat: {city.Latitude}, Lng: {city.Longitude}. " +
                    $"Error: {ex.Message}", ex);
            }
        }
    }
}

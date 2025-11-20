using DreamTravel.Trips.Domain.Cities;
using Newtonsoft.Json.Linq;

namespace DreamTravel.Trips.GeolocationDataClients.GoogleApi
{
    public partial class GoogleApiClient : IGoogleApiClient
    {
        public async Task<City> GetNameOfCity(City city)
        {
            try
            {
                string url =
                    $"https://maps.googleapis.com/maps/api/geocode/json?latlng={city.Latitude},{city.Longitude}&sensor=false&key={_options.Key}";

                string response = await _httpClient.GetStringAsync(url);
                JObject json = JObject.Parse(response);

                for (int i = 0; i < json["results"][0]["address_components"].Count(); i++)
                {
                    var component = json["results"][0]["address_components"][i];

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
                    city.Name = json["results"][0]["formatted_address"].Value<string>();
                }

                return city;
            }
            catch (Exception)
            {
                throw new InvalidDataException($"Cannot find city for coordinates: Lat: [{city.Latitude}], [{city.Longitude}]");
            }
        }
    }
}

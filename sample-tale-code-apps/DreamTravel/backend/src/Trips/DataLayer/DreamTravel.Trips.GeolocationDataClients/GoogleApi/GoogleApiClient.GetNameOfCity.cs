using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DreamTravel.Trips.Domain.Cities;
using Newtonsoft.Json.Linq;

namespace DreamTravel.GeolocationData.GoogleApi
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
                    for (int x = 0; x < json["results"][0]["address_components"][i]["types"].Count(); x++)
                    {
                        string type = json["results"][0]["address_components"][i]["types"][x].Value<string>();
                        if (type == "locality" || type == "postal_town")
                        {
                            city.Name = json["results"][0]["address_components"][i]["long_name"]
                                .Value<string>();
                        }
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

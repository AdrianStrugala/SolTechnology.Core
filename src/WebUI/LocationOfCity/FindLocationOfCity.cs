namespace DreamTravel.WebUI.LocationOfCity
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Contract;
    using Interfaces;
    using Newtonsoft.Json.Linq;

    public class FindLocationOfCity : IFindLocationOfCity
    {
        private readonly HttpClient _httpClient;

        public FindLocationOfCity()
        {
            if (_httpClient == null)
            {
                _httpClient = new HttpClient();
            }
        }

        public async Task<City> Execute(string cityName)
        {
            try
            {
                string url =
                    $"https://maps.googleapis.com/maps/api/geocode/json?address={cityName}&key=AIzaSyC8ZkBAtPiwp0rk4SRdhH6vufM39eDMmHY";

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

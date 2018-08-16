using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using DreamTravel.ExternalConnection.Interfaces;
using DreamTravel.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DreamTravel.ExternalConnection
{
    public class DownloadCityNameByLocation : IDownloadCityNameByLocation
    {
        private readonly HttpClient _httpClient;

        public DownloadCityNameByLocation()
        {
            if (_httpClient == null)
            {
                _httpClient = new HttpClient();
            }
        }

        public async Task<City> Execute(City city)
        {
            try
            {
                string url =
                    $"https://maps.googleapis.com/maps/api/geocode/json?latlng={city.Latitude},{city.Longitude}&sensor=false&key=AIzaSyBgCjCJuGQsXlAz6BUXPIL2_RSxgXUaCcM";

                HttpResponseMessage getAsync = await _httpClient.GetAsync(url);

                using (Stream stream = getAsync.Content.ReadAsStreamAsync().Result ??
                                       throw new ArgumentNullException(
                                           $"Execption on [{MethodBase.GetCurrentMethod().Name}]"))
                {
                    using (var jsonTextReader = new JsonTextReader(new StreamReader(stream)))
                    {
                        JObject json = (JObject)new JsonSerializer().Deserialize(jsonTextReader);
                        city.Name = json["results"][1]["address_components"][1]["long_name"].Value<string>();
                        return city;
                    }
                }
            }
            catch (Exception)
            {
                throw new InvalidDataException($"Cannot find city for coordinates: Lat: [{city.Latitude}], [{city.Longitude}]");
            }
        }
    }
}

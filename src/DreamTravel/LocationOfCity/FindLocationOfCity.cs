using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using DreamTravel.LocationOfCity.Interfaces;
using DreamTravel.SharedModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DreamTravel.LocationOfCity
{
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
                    $"https://maps.googleapis.com/maps/api/geocode/json?address={cityName}&key=AIzaSyBgCjCJuGQsXlAz6BUXPIL2_RSxgXUaCcM";

                City toAdd = new City { Name = cityName };

                HttpResponseMessage getAsync = await _httpClient.GetAsync(url);

                using (Stream stream = getAsync.Content.ReadAsStreamAsync().Result ??
                                       throw new ArgumentNullException(
                                           $"Execption on [{MethodBase.GetCurrentMethod().Name}]"))
                {
                    using (var jsonTextReader = new JsonTextReader(new StreamReader(stream)))
                    {
                        JObject json = (JObject)new JsonSerializer().Deserialize(jsonTextReader);
                        toAdd.Latitude = json["results"][0]["geometry"]["location"]["lat"].Value<double>();
                        toAdd.Longitude = json["results"][0]["geometry"]["location"]["lng"].Value<double>();
                        return toAdd;
                    }
                }
            }
            catch (Exception)
            {
                throw new InvalidDataException($"Cannot find city [{cityName}]");
            }
        }
    }
}

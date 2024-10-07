using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DreamTravel.GeolocationData.GeoDb.Models;

namespace DreamTravel.GeolocationData.GeoDb
{
    public class GeoDbApiClient : IGeoDbApiClient
    {
        private readonly HttpClient _httpClient;

        public GeoDbApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<CityDetails> GetCityDetails(string cityName)
        {
            var response = await _httpClient
                .CreateRequest($"v1/geo/places?namePrefix={cityName}&types=CITY&hateoasMode=false&limit=5&offset=0")
                .GetAsync<GetCityResponse>();

            return response.Data.FirstOrDefault();
        }
    }
}
using DreamTravel.GeolocationDataClients.GeoDb.Models;

namespace DreamTravel.GeolocationDataClients.GeoDb
{
    public class GeoDbApiClient : IGeoDbApiClient
    {
        private readonly HttpClient _httpClient;

        public GeoDbApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<CityDetails?> GetCityDetails(string cityName)
        {
            var response = await _httpClient
                .CreateRequest($"v1/geo/places?namePrefix={cityName}&types=CITY&hateoasMode=false&limit=5&offset=0")
                .GetAsync<GetCityResponse>();

            return response.Data.FirstOrDefault();
        }
    }
}
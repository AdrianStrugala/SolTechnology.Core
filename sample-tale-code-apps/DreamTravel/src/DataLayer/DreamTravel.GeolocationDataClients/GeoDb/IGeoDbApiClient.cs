using DreamTravel.GeolocationDataClients.GeoDb.Models;

namespace DreamTravel.GeolocationDataClients.GeoDb;

public interface IGeoDbApiClient
{
    Task<CityDetails?> GetCityDetails(string cityName);
}
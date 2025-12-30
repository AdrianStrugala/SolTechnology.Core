using DreamTravel.GeolocationDataClients.GeoDb.Models;

namespace DreamTravel.GeolocationDataClients.GeoDb;

public interface IGeoDbHTTPClient
{
    Task<CityDetails?> GetCityDetails(string cityName);
}
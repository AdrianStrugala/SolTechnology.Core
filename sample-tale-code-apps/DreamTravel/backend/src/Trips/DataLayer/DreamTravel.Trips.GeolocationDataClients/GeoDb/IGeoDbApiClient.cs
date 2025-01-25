using DreamTravel.Trips.GeolocationDataClients.GeoDb.Models;

namespace DreamTravel.Trips.GeolocationDataClients.GeoDb;

public interface IGeoDbApiClient
{
    Task<CityDetails?> GetCityDetails(string cityName);
}
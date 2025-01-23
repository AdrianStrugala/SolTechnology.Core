using DreamTravel.GeolocationData.GeoDb.Models;

namespace DreamTravel.Trips.GeolocationDataClients.GeoDb;

public interface IGeoDbApiClient
{
    Task<CityDetails?> GetCityDetails(string cityName);
}
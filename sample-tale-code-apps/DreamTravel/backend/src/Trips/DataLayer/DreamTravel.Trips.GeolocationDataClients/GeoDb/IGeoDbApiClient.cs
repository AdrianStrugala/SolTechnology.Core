using System.Threading.Tasks;
using DreamTravel.GeolocationData.GeoDb.Models;

namespace DreamTravel.GeolocationData.GeoDb;

public interface IGeoDbApiClient
{
    Task<CityDetails> GetCityDetails(string cityName);
}
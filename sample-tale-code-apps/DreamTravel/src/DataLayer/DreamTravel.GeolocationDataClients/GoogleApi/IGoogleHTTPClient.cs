using DreamTravel.Domain.Cities;
using SolTechnology.Core;

namespace DreamTravel.GeolocationDataClients.GoogleApi
{
    public interface IGoogleHTTPClient
    {
        Task<double[]> GetDurationMatrixByTollRoad(List<City> listOfCities);

        Task<double[]> GetDurationMatrixByFreeRoad(List<City> listOfCities);

        Task<TrafficMatrixResponse> GetSegmentDurationMatrixByTraffic(TrafficMatrixRequest request);

        /// <summary>Geocodes a city name; a name Google cannot match is a failed result with a NotFoundError.</summary>
        Task<Result<City>> GetLocationOfCity(string cityName);

        /// <summary>Reverse-geocodes coordinates; coordinates Google cannot match are a failed result with a NotFoundError.</summary>
        Task<Result<City>> GetNameOfCity(City city);

    }
}

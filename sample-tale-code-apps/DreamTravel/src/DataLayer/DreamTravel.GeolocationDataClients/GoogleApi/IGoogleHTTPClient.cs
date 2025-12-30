using DreamTravel.Domain.Cities;

namespace DreamTravel.GeolocationDataClients.GoogleApi
{
    public interface IGoogleHTTPClient
    {
        Task<double[]> GetDurationMatrixByTollRoad(List<City> listOfCities);

        Task<double[]> GetDurationMatrixByFreeRoad(List<City> listOfCities);

        Task<TrafficMatrixResponse> GetSegmentDurationMatrixByTraffic(TrafficMatrixRequest request);

        Task<City> GetLocationOfCity(string cityName);

        Task<City> GetNameOfCity(City city);

    }
}

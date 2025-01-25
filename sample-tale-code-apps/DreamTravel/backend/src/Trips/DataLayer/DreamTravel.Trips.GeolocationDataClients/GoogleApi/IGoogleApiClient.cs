using DreamTravel.Trips.Domain.Cities;

namespace DreamTravel.Trips.GeolocationDataClients.GoogleApi
{
    public interface IGoogleApiClient
    {
        Task<double[]> GetDurationMatrixByTollRoad(List<City> listOfCities);

        Task<double[]> GetDurationMatrixByFreeRoad(List<City> listOfCities);

        Task<City> GetLocationOfCity(string cityName);

        Task<City> GetNameOfCity(City city);
    }
}

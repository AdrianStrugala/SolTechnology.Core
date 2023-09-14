using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.Trips.Domain.Cities;

namespace DreamTravel.GeolocationData.GoogleApi
{
    public interface IGoogleApiClient
    {
        Task<double[]> GetDurationMatrixByTollRoad(List<City> listOfCities);

        Task<double[]> GetDurationMatrixByFreeRoad(List<City> listOfCities);

        Task<City> GetLocationOfCity(string cityName);

        Task<City> GetNameOfCity(City city);
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.Models;

namespace DreamTravel.ExternalConnection
{
    public interface ICallAPI
    {
        double DowloadCostBetweenTwoCities(City origin, City destination);
        Task<City> DownloadLocationOfCity(string cityName);
        double[] DowloadDurationMatrixByTollRoad(List<City> listOfCities);
        double[] DowloadDurationMatrixByFreeRoad(List<City> listOfCities);
    }
}
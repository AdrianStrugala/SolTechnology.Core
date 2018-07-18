using System.Threading.Tasks;
using DreamTravel.Models;
using Newtonsoft.Json.Linq;

namespace DreamTravel.ExternalConnection
{
    public interface ICallAPI
    {
        double DowloadCostBetweenTwoCities(City origin, City destination);
        int DowloadDurationBetweenTwoCitesByFreeRoad(City origin, City destination);
        int DowloadDurationBetweenTwoCitesByTollRoad(City origin, City destination);
        Task<City> DownloadLocationOfCity(string cityName);
    }
}
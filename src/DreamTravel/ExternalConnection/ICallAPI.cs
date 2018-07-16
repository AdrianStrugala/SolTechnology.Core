using System.Threading.Tasks;
using DreamTravel.Models;
using Newtonsoft.Json.Linq;

namespace DreamTravel.ExternalConnection
{
    public interface ICallAPI
    {
        Task<double> DowloadCostBetweenTwoCities(City origin, City destination);
        Task<int> DowloadDurationBetweenTwoCitesByFreeRoad(City origin, City destination);
        Task<int> DowloadDurationBetweenTwoCitesByTollRoad(City origin, City destination);
        Task<City> DownloadLocationOfCity(string cityName);
    }
}
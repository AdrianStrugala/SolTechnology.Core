using DreamTravel.Models;
using Newtonsoft.Json.Linq;

namespace DreamTravel.ExternalConnection
{
    public interface ICallAPI
    {
        string DowloadCostBetweenTwoCities(City origin, City destination);
        JObject DowloadDurationBetweenTwoCitesByFreeRoad(City origin, City destination);
        JObject DowloadDurationBetweenTwoCitesByTollRoad(City origin, City destination);
        JObject DownloadLocationOfCity(string cityName);
    }
}
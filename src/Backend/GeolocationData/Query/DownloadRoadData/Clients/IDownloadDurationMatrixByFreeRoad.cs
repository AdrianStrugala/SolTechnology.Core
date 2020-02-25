using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.Domain.Cities;

namespace DreamTravel.GeolocationData.Query.DownloadRoadData.Clients
{
    public interface IDownloadDurationMatrixByFreeRoad
    {
        Task<double[]> Execute(List<City> listOfCities);
    }
}

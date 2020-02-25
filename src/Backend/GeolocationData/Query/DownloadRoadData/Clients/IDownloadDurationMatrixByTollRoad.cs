using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.Domain.Cities;

namespace DreamTravel.GeolocationData.Query.DownloadRoadData.Clients
{
    public interface IDownloadDurationMatrixByTollRoad
    {
        Task<double[]> Execute(List<City> listOfCities);
    }
}

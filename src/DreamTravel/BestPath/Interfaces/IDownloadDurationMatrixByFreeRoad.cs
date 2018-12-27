using System.Collections.Generic;
using DreamTravel.SharedModels;

namespace DreamTravel.BestPath.Interfaces
{
    using System.Threading.Tasks;

    public interface IDownloadDurationMatrixByFreeRoad
    {
        double[] Execute(List<City> listOfCities);
        Task<double[]> ExecuteV2(City origin, List<City> destinations);
        Task<double[]> ExecuteV4(List<City> listOfCities);
    }
}

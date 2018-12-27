using DreamTravel.SharedModels;
using System.Collections.Generic;

namespace DreamTravel.BestPath.Interfaces
{
    using System.Threading.Tasks;

    public interface IDownloadDurationMatrixByTollRoad
    {
        double[] Execute(List<City> listOfCities);
        Task<double[]> ExecuteV2(City origin, List<City> destinations);
        Task<double[]> ExecuteV4(List<City> listOfCities);
    }
}
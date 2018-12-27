using DreamTravel.SharedModels;
using System.Collections.Generic;

namespace DreamTravel.BestPath.Interfaces
{
    using System.Threading.Tasks;

    public interface IDownloadDurationMatrixByTollRoad
    {
        Task<double[]> Execute(List<City> listOfCities);
    }
}
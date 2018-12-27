using System.Collections.Generic;
using DreamTravel.SharedModels;

namespace DreamTravel.BestPath.Interfaces
{
    public interface IDownloadDurationMatrixByFreeRoad
    {
        double[] Execute(List<City> listOfCities);
        double[] ExecuteV2(City origin, List<City> destinations);
    }
}

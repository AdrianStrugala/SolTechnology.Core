using System.Collections.Generic;
using DreamTravel.SharedModels;

namespace DreamTravel.BestPath.Interfaces
{
    public interface IDownloadDurationMatrixByFreeRoad
    {
        double[] Execute(List<City> listOfCities);
    }
}

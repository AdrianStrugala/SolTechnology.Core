using System.Collections.Generic;
using DreamTravel.SharedModels;

namespace DreamTravel.BestPath.Interfaces
{
    public interface IDownloadDurationMatrixByTollRoad
    {
        double[] Execute(List<City> listOfCities);
    }
}
using DreamTravel.SharedModels;

namespace DreamTravel.BestPath.Interfaces
{
    using System.Collections.Generic;

    public interface IDownloadCostBetweenTwoCities
    {
        (double, double) Execute(City origin, City destination);
        (double[], double[]) ExecuteV3(List<City> listOfCities);
    }
}
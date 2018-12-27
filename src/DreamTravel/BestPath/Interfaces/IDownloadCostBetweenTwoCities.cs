using DreamTravel.SharedModels;

namespace DreamTravel.BestPath.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IDownloadCostBetweenTwoCities
    {
        (double, double) Execute(City origin, City destination);
        (double[], double[]) ExecuteV3(List<City> listOfCities);
        (double, double) ExecuteV4(City origin, City destination);
    }
}
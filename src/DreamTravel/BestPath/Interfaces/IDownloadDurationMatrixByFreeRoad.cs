namespace DreamTravel.BestPath.Interfaces
{
    using System.Threading.Tasks;
    using SharedModels;
    using System.Collections.Generic;

    public interface IDownloadDurationMatrixByFreeRoad
    {
        Task<double[]> Execute(List<City> listOfCities);
    }
}

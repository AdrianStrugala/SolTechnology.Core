namespace DreamTravel.WebUI.BestPath.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Contract;

    public interface IDownloadDurationMatrixByFreeRoad
    {
        Task<double[]> Execute(List<City> listOfCities);
    }
}

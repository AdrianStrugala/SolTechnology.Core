namespace WebUI.BestPath.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using SharedModels;

    public interface IDownloadDurationMatrixByFreeRoad
    {
        Task<double[]> Execute(List<City> listOfCities);
    }
}

namespace DreamTravel.WebUI.BestPath.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using SharedModels;

    public interface IDownloadDurationMatrixByTollRoad
    {
        Task<double[]> Execute(List<City> listOfCities);
    }
}
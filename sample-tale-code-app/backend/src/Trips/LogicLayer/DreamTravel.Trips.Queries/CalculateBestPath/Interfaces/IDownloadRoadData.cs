using DreamTravel.Trips.Domain.Cities;

namespace DreamTravel.Trips.Queries.CalculateBestPath.Interfaces
{
    public interface IDownloadRoadData
    {
        Task Execute(List<City> listOfCities, CalculateBestPathContext calculateBestPathContext);
    }
}

using DreamTravel.Trips.Domain.Cities;

namespace DreamTravel.Trips.Queries.CalculateBestPath.Interfaces
{
    public interface IDownloadRoadData
    {
        Task<EvaluationMatrix> Execute(List<City> listOfCities);
    }
}

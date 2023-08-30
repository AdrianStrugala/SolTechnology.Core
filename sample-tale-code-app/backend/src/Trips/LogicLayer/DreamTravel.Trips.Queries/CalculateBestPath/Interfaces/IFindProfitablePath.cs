using DreamTravel.Trips.Domain.Cities;

namespace DreamTravel.Trips.Queries.CalculateBestPath.Interfaces
{
    public interface IFindProfitablePath
    {
        void Execute(CalculateBestPathContext calculateBestPathContext, int noOfCities);
    }
}
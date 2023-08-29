using DreamTravel.Trips.Domain.Cities;

namespace DreamTravel.DreamTrips.CalculateBestPath.Interfaces
{
    public interface IFindProfitablePath
    {
        EvaluationMatrix Execute(EvaluationMatrix evaluationMatrix, int noOfCities);
    }
}
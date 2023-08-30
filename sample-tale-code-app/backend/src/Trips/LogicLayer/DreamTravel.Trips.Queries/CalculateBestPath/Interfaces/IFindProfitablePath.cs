using DreamTravel.Trips.Domain.Cities;

namespace DreamTravel.Trips.Queries.CalculateBestPath.Interfaces
{
    public interface IFindProfitablePath
    {
        EvaluationMatrix Execute(EvaluationMatrix evaluationMatrix, int noOfCities);
    }
}
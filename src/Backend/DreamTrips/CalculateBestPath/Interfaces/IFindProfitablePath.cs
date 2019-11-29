using DreamTravel.DreamTrips.CalculateBestPath.Models;

namespace DreamTravel.DreamTrips.CalculateBestPath.Interfaces
{
    public interface IFindProfitablePath
    {
        EvaluationMatrix Execute(EvaluationMatrix evaluationMatrix, int noOfCities);
    }
}
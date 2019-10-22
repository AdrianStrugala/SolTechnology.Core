using DreamTravel.Features.DreamTrip.CalculateBestPath.Models;

namespace DreamTravel.Features.DreamTrip.CalculateBestPath.Interfaces
{
    public interface IFindProfitablePath
    {
        EvaluationMatrix Execute(EvaluationMatrix evaluationMatrix, int noOfCities);
    }
}
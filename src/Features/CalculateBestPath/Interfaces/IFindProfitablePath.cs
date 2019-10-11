using DreamTravel.Features.CalculateBestPath.Models;

namespace DreamTravel.Features.CalculateBestPath.Interfaces
{
    public interface IFindProfitablePath
    {
        EvaluationMatrix Execute(EvaluationMatrix evaluationMatrix, int noOfCities);
    }
}
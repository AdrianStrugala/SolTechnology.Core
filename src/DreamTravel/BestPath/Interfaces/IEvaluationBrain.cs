using DreamTravel.BestPath.Models;

namespace DreamTravel.BestPath.Interfaces
{
    public interface IEvaluationBrain
    {
        EvaluationMatrix Execute(EvaluationMatrix evaluationMatrix, int noOfCities);
    }
}
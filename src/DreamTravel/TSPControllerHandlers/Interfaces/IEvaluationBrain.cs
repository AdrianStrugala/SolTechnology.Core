using DreamTravel.Models;

namespace DreamTravel.TSPControllerHandlers.Interfaces
{
    public interface IEvaluationBrain
    {
        EvaluationMatrix Execute(EvaluationMatrix evaluationMatrix, int noOfCities);
    }
}
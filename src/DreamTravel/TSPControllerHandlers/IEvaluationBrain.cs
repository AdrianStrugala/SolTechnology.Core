using DreamTravel.Models;

namespace DreamTravel.TSPControllerHandlers
{
    public interface IEvaluationBrain
    {
        EvaluationMatrix EvaluateCost(EvaluationMatrix evaluationMatrix, int noOfCities);
    }
}
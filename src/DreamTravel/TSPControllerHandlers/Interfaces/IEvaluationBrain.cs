using DreamTravel.Models;

namespace DreamTravel.TSPControllerHandlers
{
    public interface IEvaluationBrain
    {
        EvaluationMatrix Execute(EvaluationMatrix evaluationMatrix, int noOfCities);
    }
}
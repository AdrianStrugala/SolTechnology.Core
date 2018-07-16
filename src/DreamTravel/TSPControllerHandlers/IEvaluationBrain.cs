using System.Threading.Tasks;
using DreamTravel.Models;

namespace DreamTravel.TSPControllerHandlers
{
    public interface IEvaluationBrain
    {
        Task<EvaluationMatrix> EvaluateCostAsync(EvaluationMatrix evaluationMatrix, int noOfCities);
    }
}
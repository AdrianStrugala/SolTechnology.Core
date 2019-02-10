namespace WebUI.BestPath.Interfaces
{
    using Models;

    public interface IEvaluationBrain
    {
        EvaluationMatrix Execute(EvaluationMatrix evaluationMatrix, int noOfCities);
    }
}
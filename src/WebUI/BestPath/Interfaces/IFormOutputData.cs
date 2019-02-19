namespace DreamTravel.WebUI.BestPath.Interfaces
{
    using System.Collections.Generic;
    using SharedModels;

    public interface IFormOutputData
    {
        List<Path> Execute(List<City> listOfCities, IEvaluationMatrix evaluationMatrix, List<int> orderOfCities = null);
    }
}
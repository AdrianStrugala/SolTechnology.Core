using DreamTravel.WebUI.BestPath.Models;

namespace DreamTravel.WebUI.BestPath.Interfaces
{
    using System.Collections.Generic;
    using Contract;

    public interface IFormOutputData
    {
        List<Path> Execute(List<City> listOfCities, EvaluationMatrix evaluationMatrix, List<int> orderOfCities = null);
    }
}
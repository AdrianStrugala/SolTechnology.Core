using System.Collections.Generic;
using DreamTravel.SharedModels;

namespace DreamTravel.BestPath.Interfaces
{
    public interface IFormOutputData
    {
        List<Path> Execute(List<City> listOfCities, IEvaluationMatrix evaluationMatrix, List<int> orderOfCities = null);
    }
}
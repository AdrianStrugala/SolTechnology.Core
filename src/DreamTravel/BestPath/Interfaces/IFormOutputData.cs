using System.Collections.Generic;
using DreamTravel.SharedModels;

namespace DreamTravel.BestPath.Interfaces
{
    public interface IFormOutputData
    {
        List<Path> Execute(List<City> listOfCities, List<int> orderOfCities, IEvaluationMatrix evaluationMatrix);
    }
}
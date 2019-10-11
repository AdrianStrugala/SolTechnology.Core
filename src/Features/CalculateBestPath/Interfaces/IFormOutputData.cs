using System.Collections.Generic;
using DreamTravel.Domain.Cities;
using DreamTravel.Domain.Paths;
using DreamTravel.Features.CalculateBestPath.Models;

namespace DreamTravel.Features.CalculateBestPath.Interfaces
{
    public interface IFormOutputData
    {
        List<Path> Execute(List<City> listOfCities, EvaluationMatrix evaluationMatrix, List<int> orderOfCities = null);
    }
}
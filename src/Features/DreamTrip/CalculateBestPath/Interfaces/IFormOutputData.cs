using System.Collections.Generic;
using DreamTravel.Domain.Cities;
using DreamTravel.Domain.Paths;
using DreamTravel.Features.DreamTrip.CalculateBestPath.Models;

namespace DreamTravel.Features.DreamTrip.CalculateBestPath.Interfaces
{
    public interface IFormPathsFromMatrices
    {
        List<Path> Execute(List<City> listOfCities, EvaluationMatrix evaluationMatrix, List<int> orderOfCities = null);
    }
}
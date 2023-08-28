using System.Collections.Generic;
using DreamTravel.Domain.Cities;
using DreamTravel.Domain.Paths;

namespace DreamTravel.DreamTrips.CalculateBestPath.Interfaces
{
    public interface IFormPathsFromMatrices
    {
        List<Path> Execute(List<City> listOfCities, EvaluationMatrix evaluationMatrix, List<int> orderOfCities = null);
    }
}
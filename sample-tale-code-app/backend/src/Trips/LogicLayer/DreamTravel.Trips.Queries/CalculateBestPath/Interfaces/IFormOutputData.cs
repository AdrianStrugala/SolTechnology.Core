using DreamTravel.Trips.Domain.Cities;
using Path = DreamTravel.Trips.Domain.Paths.Path;

namespace DreamTravel.Trips.Queries.CalculateBestPath.Interfaces
{
    public interface IFormPathsFromMatrices
    {
        List<Path> Execute(List<City> listOfCities, EvaluationMatrix evaluationMatrix, List<int> orderOfCities = null);
    }
}
using DreamTravel.Trips.Domain.Cities;
using Path = DreamTravel.Trips.Domain.Paths.Path;

namespace DreamTravel.Trips.Queries.CalculateBestPath;

public class CalculateBestPathResult
{
    public List<Path> BestPaths { get; set; } = new();
    public List<City> Cities { get; set; } = new();
}
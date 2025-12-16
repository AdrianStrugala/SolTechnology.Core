using DreamTravel.Domain.Cities;
using Path = DreamTravel.Domain.Paths.Path;

namespace DreamTravel.Queries.CalculateBestPath;

public class CalculateBestPathResult
{
    public List<Path> BestPaths { get; set; } = new();
    public List<City> Cities { get; set; } = new();
}
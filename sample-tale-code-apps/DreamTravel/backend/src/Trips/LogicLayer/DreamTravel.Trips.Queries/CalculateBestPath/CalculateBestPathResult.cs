using DreamTravel.Trips.Domain.Cities;
using Path = DreamTravel.Trips.Domain.Paths.Path;

namespace DreamTravel.Trips.Queries.CalculateBestPath;

public class CalculateBestPathResult
{
    public List<Path> BestPaths { get; set; }
    public List<City> Cities { get; set; }

    public CalculateBestPathResult()
    {
        BestPaths = new List<Path>();
        Cities = new List<City>();
    }
}
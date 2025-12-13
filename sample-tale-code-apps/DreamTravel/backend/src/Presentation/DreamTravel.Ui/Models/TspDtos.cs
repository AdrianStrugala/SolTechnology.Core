using DreamTravel.Trips.Domain.Cities;
using Path = DreamTravel.Trips.Domain.Paths.Path;

namespace DreamTravel.Ui.Models;

// DTOs for TSP API communication
// These mirror the server-side query/result models but are client-safe

public class CalculateBestPathQuery
{
    public List<City> Cities { get; set; } = new();
}

public class CalculateBestPathResult
{
    public List<Path> BestPaths { get; set; } = new();
    public List<City> Cities { get; set; } = new();
}

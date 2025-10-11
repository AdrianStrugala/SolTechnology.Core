namespace DreamTravel.Trips.Domain.SearchStatistics;

public class CityStatistics
{
    public required string CityName { get; set; }
    public int SearchCount { get; set; }
    public string Country { get; set; } = null!;
}
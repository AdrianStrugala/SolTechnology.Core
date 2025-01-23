namespace DreamTravel.Trips.Domain.SearchStatistics;

public class CountryStatistics
{
    public required string Country { get; set; }
    public int TotalSearchCount { get; set; }
    public List<CityStatistics> CityStatistics { get; set; } = new();
}
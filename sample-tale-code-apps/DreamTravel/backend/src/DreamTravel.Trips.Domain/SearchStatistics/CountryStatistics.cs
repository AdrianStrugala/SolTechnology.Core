namespace DreamTravel.Trips.Domain.SearchStatistics;

public class CountryStatistics
{
    public required string Country { get; set; }
    public int TotalSearchCount { get; set; }
}
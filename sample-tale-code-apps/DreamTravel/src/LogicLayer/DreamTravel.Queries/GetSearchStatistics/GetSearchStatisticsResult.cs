namespace DreamTravel.Queries.GetSearchStatistics;

public class GetSearchStatisticsResult
{
    public List<CountryStatistics> CountryStatistics { get; set; } = new();
}

public class CountryStatistics
{
    public required string Country { get; set; }
    public int TotalSearchCount { get; set; }
}









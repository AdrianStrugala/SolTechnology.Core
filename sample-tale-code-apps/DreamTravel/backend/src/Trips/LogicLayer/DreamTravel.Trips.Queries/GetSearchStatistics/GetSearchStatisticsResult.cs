using DreamTravel.Trips.Domain.SearchStatistics;

namespace DreamTravel.Trips.Queries.GetSearchStatistics;

public class GetSearchStatisticsResult
{
    public List<CountryStatistics> CountryStatistics { get; set; } = new();
}







namespace DreamTravel.Trips.Domain.SearchStatistics;

public class CityStatistics
{
    public long Id { get; set; }
    public long CityId { get; set; }
    public int SearchCount { get; set; }
}
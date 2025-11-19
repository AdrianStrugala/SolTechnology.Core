namespace DreamTravel.Trips.Sql.DbModels;

public record CityStatisticsEntity : BaseEntity
{
    public long CityId { get; set; }
    public int SearchCount { get; set; }
    public DateOnly Date { get; set; }
    
    public CityEntity City { get; set; } = null!;
}
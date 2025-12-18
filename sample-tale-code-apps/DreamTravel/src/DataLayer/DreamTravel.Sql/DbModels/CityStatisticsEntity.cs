namespace DreamTravel.Sql.DbModels;

public record CityStatisticsEntity : BaseEntity
{
    public int SearchCount { get; set; }
    public DateOnly Date { get; set; }
    
    public CityEntity City { get; set; } = null!;
}
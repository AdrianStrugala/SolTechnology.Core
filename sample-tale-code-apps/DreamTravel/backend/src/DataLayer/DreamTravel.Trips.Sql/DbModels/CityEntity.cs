namespace DreamTravel.Trips.Sql.DbModels;

public record CityEntity : BaseEntity
{
    public Auid CityId { get; set; }          // biznesowe Id (unikalne)
    
    public required double Latitude { get; set; }
    public required double Longitude { get; set; }
    public required string Country { get; set; } 

    public List<AlternativeNameEntity> AlternativeNames { get; set; } = [];
    public List<CityStatisticsEntity> Statistics { get; set; } = [];
}
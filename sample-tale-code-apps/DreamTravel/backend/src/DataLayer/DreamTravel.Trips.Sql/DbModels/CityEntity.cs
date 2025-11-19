namespace DreamTravel.Trips.Sql.DbModels;

public record CityEntity : BaseEntity
{
    public Guid CityId { get; set; }          // biznesowe Id (unikalne)
    
    public required double Latitude { get; set; }
    public required double Longitude { get; set; }
    public string? Country { get; set; } 
    public string? Region { get; set; }
    public int? Population { get; set; }

    public List<AlternativeNameEntity> AlternativeNames { get; set; } = [];
    public List<CityStatisticsEntity> Statistics { get; set; } = [];
}
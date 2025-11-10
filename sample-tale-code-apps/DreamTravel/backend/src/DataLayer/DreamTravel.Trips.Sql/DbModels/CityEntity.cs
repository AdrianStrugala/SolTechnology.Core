using DreamTravel.Trips.Domain;

namespace DreamTravel.Trips.Sql.DbModels;

public record CityEntity : EntityBase
{
    public required string Name { get; set; }
    public required double Latitude { get; set; }
    public required double Longitude { get; set; }
    public string? Country { get; set; } 
    public string? Region { get; set; }
    public int? Population { get; set; }

    public List<AlternativeNameEntity> AlternativeNames { get; set; } = [];
}
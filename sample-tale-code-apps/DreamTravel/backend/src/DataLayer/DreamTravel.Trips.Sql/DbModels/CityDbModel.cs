using DreamTravel.Trips.Domain;
using DreamTravel.Trips.Domain.Cities;

namespace DreamTravel.Trips.Sql.DbModels;

public record CityDbModel : EntityBase
{
    public required string Name { get; set; }
    public required double Latitude { get; set; }
    public required double Longitude { get; set; }
    public required string Country { get; set; }
    public string? Region { get; set; }
    public int? Population { get; set; }

    public List<string> AlternativeNames { get; set; } = [];
}
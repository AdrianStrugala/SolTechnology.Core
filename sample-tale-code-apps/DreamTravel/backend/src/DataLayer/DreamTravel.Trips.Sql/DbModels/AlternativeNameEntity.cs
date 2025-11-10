using DreamTravel.Trips.Domain;

namespace DreamTravel.Trips.Sql.DbModels;

public record AlternativeNameEntity : EntityBase
{
    public long CityId { get; set; }
    public required string AlternativeName { get; set; }
}
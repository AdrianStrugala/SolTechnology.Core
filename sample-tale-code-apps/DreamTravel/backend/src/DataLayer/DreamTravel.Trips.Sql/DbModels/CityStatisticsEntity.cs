using DreamTravel.Trips.Domain;

namespace DreamTravel.Trips.Sql.DbModels;

public record CityStatisticsEntity : EntityBase
{
    public long CityId { get; set; }
    public int SearchCount { get; set; }
}
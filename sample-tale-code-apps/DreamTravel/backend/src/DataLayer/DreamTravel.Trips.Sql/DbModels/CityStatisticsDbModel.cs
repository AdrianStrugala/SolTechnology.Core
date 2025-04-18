using DreamTravel.Trips.Domain;

namespace DreamTravel.Trips.Sql.DbModels;

public record CityStatisticsDbModel : EntityBase
{
    public long CityId { get; set; }
    public int SearchCount { get; set; }
}
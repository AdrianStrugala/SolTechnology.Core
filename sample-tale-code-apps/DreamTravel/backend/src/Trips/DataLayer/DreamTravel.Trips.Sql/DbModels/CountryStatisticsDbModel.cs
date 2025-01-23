using DreamTravel.Trips.Domain;

namespace DreamTravel.Trips.Sql.DbModels;

public record CountryStatisticsDbModel : EntityBase
{
    public string Country { get; init; } = null!;
    public int TotalSearchCount { get; init; }
}
namespace DreamTravel.Trips.Sql.DbModels;

public record CountryStatisticsEntity
{
    public string Country { get; init; } = null!;
    public int TotalSearchCount { get; init; }
}
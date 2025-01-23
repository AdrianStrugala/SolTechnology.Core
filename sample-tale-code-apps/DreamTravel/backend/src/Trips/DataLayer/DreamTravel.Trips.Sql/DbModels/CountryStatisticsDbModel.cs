namespace DreamTravel.Trips.Sql.DbModels;

public record CountryStatisticsDbModel
{
    public string Country { get; init; } = null!;
    public int TotalSearchCount { get; init; }
}
namespace DreamTravel.Trips.Domain.Cities;

public record City
{
    public string Name { get; set; } = null!;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Country { get; set; } = null!;

    public IReadOnlyList<CitySearchStatistics> SearchStatistics { get; set; } = null!;
    
    // helper property
    public CityReadOptions ReadOptions { get; set; } = CityReadOptions.Default;
}
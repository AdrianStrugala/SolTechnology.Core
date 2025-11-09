namespace DreamTravel.Trips.Domain.Cities;

public record City
{
    public string Name { get; set; } = null!;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
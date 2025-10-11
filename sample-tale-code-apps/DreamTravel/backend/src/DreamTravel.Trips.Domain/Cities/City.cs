namespace DreamTravel.Trips.Domain.Cities;

public record City : EntityBase
{
    public string Name { get; set; } = null!;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
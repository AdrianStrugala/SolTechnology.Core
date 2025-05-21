namespace DreamTravel.Trips.Domain.StreetGraph;

public class Intersection
{
    public string Id  { get; set; } = default!;  // NodeId
    public double Lat { get; set; }
    public double Lng { get; set; }
}
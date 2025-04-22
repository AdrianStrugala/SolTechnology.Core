namespace DreamTravel.Trips.Domain.StreetGraph;

public class Street
{
    public string Id       { get; set; } = default!;  // RelationshipId
    public string FromId   { get; set; } = default!;
    public string ToId     { get; set; } = default!;
    public string? Name    { get; set; }
    public double Length   { get; set; }
    public int? Lanes      { get; set; }
    public string? TurnLanes { get; set; }
}
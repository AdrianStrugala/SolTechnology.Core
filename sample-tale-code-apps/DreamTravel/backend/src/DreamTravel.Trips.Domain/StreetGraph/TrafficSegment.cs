namespace DreamTravel.Trips.Domain.StreetGraph;

public record TrafficSegment(
    string SegmentId,
    double FromLat,
    double FromLng,
    double ToLat,
    double ToLng
)
{
    public double DurationInSeconds { get; set; }
    public double DistanceInMeters { get; set; }
    public double? TrafficRegularSpeed { get; set; }
}
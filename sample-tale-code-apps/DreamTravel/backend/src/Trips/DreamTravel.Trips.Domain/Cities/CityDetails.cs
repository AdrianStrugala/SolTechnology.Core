namespace DreamTravel.Trips.Domain.Cities
{
    public record CityDetails : City
    {
        public string Country { get; set; } = null!;
        public string? Region { get; set; }
        public int? Population { get; set; }
    }
}

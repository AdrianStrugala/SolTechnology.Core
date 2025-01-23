using System.Text.Json.Serialization;

namespace DreamTravel.Trips.Domain.SearchStatistics;

public class CityStatistics
{
    public required string CityName { get; set; }
    public int SearchCount { get; set; }
    [JsonIgnore]
    public required string Country { get; set; }
}
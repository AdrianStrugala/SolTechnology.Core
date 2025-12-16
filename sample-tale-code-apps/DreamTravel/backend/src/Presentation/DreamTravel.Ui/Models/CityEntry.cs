using DreamTravel.Trips.Domain.Cities;

namespace DreamTravel.Ui.Models;

public class CityEntry
{
    public int Index { get; set; }
    public string Name { get; set; } = string.Empty;
    public City? City { get; set; }
    public bool IsSearching { get; set; }
}

public class MapClickEventArgs
{
    public double Lat { get; set; }
    public double Lng { get; set; }
}

namespace DreamTravel.DomainServices.CityDomain.SaveCityStory;

/// <summary>
/// Result for SaveCityStory - indicates whether the city was saved successfully.
/// </summary>
public class SaveCityResult
{
    /// <summary>
    /// Indicates whether this was a new city (true) or an update (false).
    /// </summary>
    public bool IsNew { get; set; }
}

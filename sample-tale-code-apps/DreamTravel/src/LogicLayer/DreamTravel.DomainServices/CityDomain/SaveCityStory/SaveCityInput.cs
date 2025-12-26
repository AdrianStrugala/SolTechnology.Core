using DreamTravel.Domain.Cities;

namespace DreamTravel.DomainServices.CityDomain.SaveCityStory;

/// <summary>
/// Input for SaveCityStory - contains the city to save.
/// </summary>
public class SaveCityInput
{
    /// <summary>
    /// The city to save to the database.
    /// </summary>
    public City City { get; set; } = null!;
}

using DreamTravel.Sql.DbModels;
using SolTechnology.Core.Tale;

namespace DreamTravel.DomainServices.CityDomain.SaveCityTale;

/// <summary>
/// Context for SaveCityTale - holds the context for saving a city.
/// </summary>
public class SaveCityContext : Context<SaveCityInput, SaveCityResult>
{
    /// <summary>
    /// The city entity to save (existing or new).
    /// </summary>
    public CityEntity CityEntity { get; set; } = null!;

    /// <summary>
    /// The date of the save operation (for statistics tracking).
    /// </summary>
    public DateOnly Today { get; set; }

    /// <summary>
    /// Indicates whether this is a new city or an update to an existing one.
    /// </summary>
    public bool IsNew { get; set; }
}

using DreamTravel.Sql.DbModels;

namespace DreamTravel.DomainServices.CityDomain.SaveSteps;

/// <summary>
/// Step responsible for assigning alternative names to a city entity.
/// </summary>
public interface IAssignAlternativeNameStep
{
    /// <summary>
    /// Assigns an alternative name to the city entity if it doesn't already exist.
    /// </summary>
    /// <param name="cityEntity">The city entity to modify.</param>
    /// <param name="name">The alternative name to add.</param>
    public void Invoke(CityEntity cityEntity, string name);
}

/// <summary>
/// Implements the logic for assigning alternative names to city entities.
/// This step modifies the tracked entity without immediately saving to the database.
/// </summary>
public class AssignAlternativeNameStep : IAssignAlternativeNameStep
{
    /// <summary>
    /// Adds an alternative name to the city entity if it doesn't already exist.
    /// Does not save to the database immediately - only modifies the tracked entity.
    /// </summary>
    /// <param name="cityEntity">The city entity to modify.</param>
    /// <param name="name">The alternative name to add.</param>
    public void Invoke(CityEntity cityEntity, string name)
    {
        var exists = cityEntity.AlternativeNames
            .Any(an => an.AlternativeName.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (exists)
            return;

        cityEntity.AlternativeNames.Add(new AlternativeNameEntity
        {
            AlternativeName = name,
            City = cityEntity 
        });
    }    
}
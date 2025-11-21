using DreamTravel.Trips.Sql.DbModels;

namespace DreamTravel.DomainServices.CityDomain.SaveSteps;

public interface IAssignAlternativeNameStep
{
    public void Invoke(CityEntity cityEntity, string name);
}

public class AssignAlternativeNameStep : IAssignAlternativeNameStep
{
    /// <summary>
    /// 1) Dodaj alternative name, jeśli nie istnieje.
    /// Nie zapisuje od razu do bazy (tylko modyfikuje tracked entity).
    /// </summary>
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
using DreamTravel.Trips.Sql.DbModels;

namespace DreamTravel.Trips.Commands.DomainServices.CityDomain;

public interface ICityExtendedBuilder
{
    CityExtendedBuilder SetCountry(string country);
    CityExtendedBuilder SetRegion(string region);
    CityExtendedBuilder SetPopulation(int population);
    IReadOnlyList<PropertyChange<CityEntity>> GetChanges();
    bool HasChanges();
}

public class CityExtendedBuilder : ICityExtendedBuilder
{
    private readonly List<PropertyChange<CityEntity>> _changes = new();

    public CityExtendedBuilder SetCountry(string country)
    {
        _changes.Add(new PropertyChange<CityEntity>(
            entity => entity.Country = country,
            country
        ));
        return this;
    }

    public CityExtendedBuilder SetRegion(string region)
    {
        _changes.Add(new PropertyChange<CityEntity>(
            entity => entity.Region = region,
            region
        ));
        return this;
    }

    public CityExtendedBuilder SetPopulation(int population)
    {
        _changes.Add(new PropertyChange<CityEntity>(
            entity => entity.Population = population,
            population
        ));
        return this;
    }

    public IReadOnlyList<PropertyChange<CityEntity>> GetChanges() => _changes;

    public bool HasChanges() => _changes.Count > 0;
}

public class PropertyChange<TDbEntity>
{
    private readonly Action<TDbEntity> _applyAction;

    internal PropertyChange(
        Action<TDbEntity> applyAction,
        object? value)
    {
        _applyAction = applyAction;
        Value = value;
    }

    public void Apply(TDbEntity entity) => _applyAction(entity);

    public object? Value { get; }
}
using DreamTravel.Trips.Sql.DbModels;

namespace DreamTravel.Trips.Commands.DomainServices.CityDomain;

public interface ICityMapper
{
    Domain.Cities.City ToDomain(CityEntity entity);
    void ApplyUpdate(CityEntity? entity, Domain.Cities.City city);
}

public class CityMapper(ICityExtendedBuilder extendedBuilder) : ICityMapper
{
    public Domain.Cities.City ToDomain(CityEntity entity)
    {
        return new Domain.Cities.City
        {
            Name = entity.Name,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude
        };
    }
    
    public void ApplyUpdate(CityEntity? entity, Domain.Cities.City city)
    {
        entity ??= new CityEntity
        {
            Name = city.Name,
            Latitude = city.Latitude,
            Longitude = city.Longitude
        };
        
        entity.Name = city.Name;
        entity.Latitude = city.Latitude;
        entity.Longitude = city.Longitude;
        
        foreach (var change in extendedBuilder.GetChanges())
        {
            change.Apply(entity);
        }
    }
}
using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Sql.DbModels;

namespace DreamTravel.Trips.Commands.DomainServices.CityDomain;

public interface ICityMapper
{
    City ToDomain(CityEntity baseEntity, string name);
    void ApplyUpdate(CityEntity? entity, City city);
}

public class CityMapper() : ICityMapper
{
    public City ToDomain(CityEntity baseEntity, string name)
    {
        return new City
        {
            Name =  name,
            Latitude = baseEntity.Latitude,
            Longitude = baseEntity.Longitude
        };
    }
    
    public void ApplyUpdate(CityEntity? entity, City city)
    {
        entity ??= new CityEntity
        {
            Latitude = city.Latitude,
            Longitude = city.Longitude
        };
        
        entity.Latitude = city.Latitude;
        entity.Longitude = city.Longitude;
    }
}
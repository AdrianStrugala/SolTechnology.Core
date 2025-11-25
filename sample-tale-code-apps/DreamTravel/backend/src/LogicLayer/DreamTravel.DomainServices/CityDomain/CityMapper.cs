using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Sql.DbModels;

namespace DreamTravel.DomainServices.CityDomain;

public interface ICityMapper
{
    City ToDomain(CityEntity entity, CityReadOptions options, string? name = null);
    CityEntity ApplyUpdate(CityEntity? entity, City city);
}

public class CityMapper : ICityMapper
{
    public City ToDomain(CityEntity entity, CityReadOptions options, string? requestedName = null)
    {
        // Jeśli user szukał po nazwie - zwróć tę nazwę
        // Jeśli szukał po lokalizacji - zwróć pierwsze name
        var cityName = requestedName 
                       ?? entity.AlternativeNames.FirstOrDefault()?.AlternativeName
                       ?? throw new InvalidOperationException("City has no name");
        
        var city = new City
        {
            Name = cityName,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
            Country = entity.Country
        };
        
        // Map statistics only if options include them
        if (entity.Statistics.Any())
        {
            city.SearchStatistics = entity.Statistics
                .Select(s => new CitySearchStatistics
                {
                    Date = s.Date,
                    SearchCount = s.SearchCount
                })
                .ToList();
        }
        
        return city;
    }
    
    public CityEntity ApplyUpdate(CityEntity? entity, City city)
    {
        if (entity == null)
        {
            return new CityEntity
            {
                CityId = Guid.NewGuid(),
                Latitude = city.Latitude,
                Longitude = city.Longitude,
                Country = city.Country
            };
        }
        
        entity.Latitude = city.Latitude;
        entity.Longitude = city.Longitude;
        entity.Country = city.Country;
        
        return entity;
    }
}
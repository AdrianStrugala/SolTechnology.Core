using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Sql.DbModels;

namespace DreamTravel.Trips.Commands.DomainServices.CityDomain;

public interface ICityMapper
{
    City ToDomain(CityEntity entity, string name);
    void ApplyUpdate(CityEntity? entity, City city);
}

public class CityMapper() : ICityMapper
{
    public City ToDomain(CityEntity entity, string name)
    {
        var city = new City
        {
            Name =  name,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude
        };
        
        // Map statistics only if options include them
        var options = city.ReadOptions;
        if (options.Statistics != null)
        {
            var statistics = entity.Statistics.AsQueryable();

            // Apply filtering (already done in EF query, but double-check)
            if (options.Statistics.From.HasValue)
                statistics = statistics.Where(s => s.Date >= options.Statistics.From.Value);

            if (options.Statistics.To.HasValue)
                statistics = statistics.Where(s => s.Date <= options.Statistics.To.Value);

            city.SearchStatistics = statistics
                .Select(s => new CitySearchStatistics
                {
                    Date = s.Date,
                    SearchCount = s.SearchCount
                })
                .ToList();
        }
        
        return city;
    }
    
    public void ApplyUpdate(CityEntity? entity, City city)
    {
        entity ??= new CityEntity
        {
            CityId = Guid.NewGuid(), 
            Latitude = city.Latitude,
            Longitude = city.Longitude
        };
        
        entity.Latitude = city.Latitude;
        entity.Longitude = city.Longitude;
    }
}
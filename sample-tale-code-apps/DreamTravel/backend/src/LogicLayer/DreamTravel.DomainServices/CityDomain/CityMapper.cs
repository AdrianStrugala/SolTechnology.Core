using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Sql.DbModels;

namespace DreamTravel.DomainServices.CityDomain;

/// <summary>
/// Mapper for converting between city domain models and database entities.
/// </summary>
public interface ICityMapper
{
    /// <summary>
    /// Maps a city entity from the database to the domain model.
    /// </summary>
    /// <param name="entity">The city entity from the database.</param>
    /// <param name="options">Read options that determine which related data to include.</param>
    /// <param name="name">Optional name to use (typically from the search request).</param>
    /// <returns>The city domain model.</returns>
    City ToDomain(CityEntity entity, CityReadOptions options, string? name = null);

    /// <summary>
    /// Applies updates from a domain model to a city entity.
    /// </summary>
    /// <param name="entity">The existing entity to update, or null to create a new one.</param>
    /// <param name="city">The domain model containing the updates.</param>
    /// <returns>The updated or newly created city entity.</returns>
    CityEntity ApplyUpdate(CityEntity? entity, City city);
}

/// <summary>
/// Implements mapping logic between city domain models and database entities.
/// </summary>
public class CityMapper : ICityMapper
{
    /// <summary>
    /// Maps a city entity from the database to the domain model.
    /// If the user searched by name, returns that name.
    /// If the user searched by coordinates, returns the first alternative name.
    /// </summary>
    /// <param name="entity">The city entity from the database.</param>
    /// <param name="options">Read options that determine which related data to include.</param>
    /// <param name="requestedName">Optional name from the search request.</param>
    /// <returns>The city domain model.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the city has no name.</exception>
    public City ToDomain(CityEntity entity, CityReadOptions options, string? requestedName = null)
    {
        // If user searched by name - return that name
        // If user searched by location - return first alternative name
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

    /// <summary>
    /// Applies updates from a domain model to a city entity.
    /// If the entity is null, creates a new city entity with a new GUID.
    /// Otherwise, updates the existing entity's properties.
    /// </summary>
    /// <param name="entity">The existing entity to update, or null to create a new one.</param>
    /// <param name="city">The domain model containing the updates.</param>
    /// <returns>The updated or newly created city entity.</returns>
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
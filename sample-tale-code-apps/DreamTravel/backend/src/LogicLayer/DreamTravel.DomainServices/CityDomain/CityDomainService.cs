using DreamTravel.DomainServices.CityDomain.SaveSteps;
using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.GeolocationDataClients.GoogleApi;
using DreamTravel.Trips.Sql;
using DreamTravel.Trips.Sql.QueryBuilders;
using Microsoft.EntityFrameworkCore;

namespace DreamTravel.DomainServices.CityDomain;

/// <summary>
/// Domain service for managing city entities.
/// Provides methods to retrieve cities from the database or external APIs,
/// and to save city information with statistics tracking.
/// </summary>
public interface ICityDomainService
{
    /// <summary>
    /// Retrieves a city by its name.
    /// If the city exists in the database, returns it with the requested options.
    /// Otherwise, fetches the city information from Google API.
    /// </summary>
    /// <param name="name">The name of the city to retrieve.</param>
    /// <param name="configureOptions">Optional configuration for what data to include (e.g., statistics).</param>
    /// <returns>The city domain model.</returns>
    Task<City> Get(string name, Action<CityReadOptions>? configureOptions = null);

    /// <summary>
    /// Retrieves a city by its geographic coordinates.
    /// If the city exists in the database, returns it with the requested options.
    /// Otherwise, fetches the city information from Google API.
    /// </summary>
    /// <param name="latitude">The latitude of the city.</param>
    /// <param name="longitude">The longitude of the city.</param>
    /// <param name="configureOptions">Optional configuration for what data to include (e.g., statistics).</param>
    /// <returns>The city domain model.</returns>
    Task<City> Get(double latitude, double longitude, Action<CityReadOptions>? configureOptions = null);

    /// <summary>
    /// Saves a city to the database.
    /// If the city already exists (based on coordinates), updates it.
    /// Otherwise, creates a new city entity.
    /// Also tracks the alternative name and increments search statistics.
    /// </summary>
    /// <param name="city">The city to save.</param>
    Task Save(City city);
}

/// <summary>
/// Implements domain service for managing city entities.
/// Orchestrates city retrieval from database or external APIs,
/// and manages city persistence with statistics tracking.
/// </summary>
public class CityDomainService(
    ICityMapper cityMapper,
    IAssignAlternativeNameStep assignAlternativeNameStep,
    IIncrementSearchCountStep incrementSearchCountStep,
    DreamTripsDbContext dbContext,
    IGoogleApiClient googleApiClient)
    : ICityDomainService
{
    public async Task<City> Get(string name, Action<CityReadOptions>? configureOptions = null)
    {
        City result;
        
        var options = CityReadOptions.Default;
        configureOptions?.Invoke(options);
        
        var cityEntity = await dbContext.Cities
            .ApplyReadOptions(options)
            .Include(c => c.AlternativeNames)
            .WhereName(name)
            .FirstOrDefaultAsync();

        if (cityEntity != null)
        {
            result = cityMapper.ToDomain(cityEntity, options, name);
        }
        else
        {
            result = await googleApiClient.GetLocationOfCity(name);
        }
        
        result.ReadOptions = options;

        return result;
    }

    public async Task<City> Get(double latitude, double longitude, Action<CityReadOptions>? configureOptions = null)
    {
        City result;
        
        var options = CityReadOptions.Default;
        configureOptions?.Invoke(options);
        
        var cityEntity = await dbContext.Cities
            .ApplyReadOptions(options)
            .Include(c => c.AlternativeNames)
            .WhereCoordinates(latitude, longitude)
            .FirstOrDefaultAsync();

        if (cityEntity != null)
        {
            result = cityMapper.ToDomain(cityEntity, options);
        }
        else
        {
            result = await googleApiClient.GetNameOfCity(new City
            {
                Latitude = latitude,
                Longitude = longitude
            });
        }
        
        result.ReadOptions = options;

        return result;
    }

    public async Task Save(City city)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        city.ReadOptions = city.ReadOptions.WithStatistics();

        var existingEntity = await dbContext.Cities
            .ApplyReadOptions(city.ReadOptions)
            .Include(c => c.AlternativeNames)
            .WhereCoordinates(city.Latitude, city.Longitude)
            .FirstOrDefaultAsync();
    
        bool isNew = existingEntity == null;

        var entityToSave = cityMapper.ApplyUpdate(existingEntity, city);
        
        assignAlternativeNameStep.Invoke(entityToSave, city.Name);
        incrementSearchCountStep.Invoke(entityToSave, today);

        if (isNew)
        {
            await dbContext.Cities.AddAsync(entityToSave);
        }
        else
        {
            dbContext.Update(entityToSave);
        }
        
        await dbContext.SaveChangesAsync();
    }
}
using DreamTravel.DomainServices.CityDomain.SaveSteps;
using DreamTravel.DomainServices.CityDomain.SaveCityStory;
using DreamTravel.DomainServices.CityDomain.SaveCityStory.Chapters;
using DreamTravel.Domain.Cities;
using DreamTravel.GeolocationDataClients.GoogleApi;
using DreamTravel.Sql;
using DreamTravel.Sql.QueryBuilders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Story;

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
    DreamTripsDbContext dbContext,
    IGoogleHTTPClient googleHTTPClient,
    IServiceProvider serviceProvider,
    ILogger<CityDomainService> logger)
    : StoryHandler<SaveCityInput, SaveCityContext, SaveCityResult>(serviceProvider, logger), ICityDomainService
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
            result = await googleHTTPClient.GetLocationOfCity(name);
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
            result = await googleHTTPClient.GetNameOfCity(new City
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
        var input = new SaveCityInput { City = city };
        var result = await Handle(input);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to save city: {result.Error?.Message}");
        }
    }

    /// <summary>
    /// Story for saving a city to the database.
    /// Orchestrates loading existing city, assigning alternative name,
    /// incrementing search count, and saving to database.
    /// </summary>
    protected override async Task TellStory()
    {
        var city = Context.Input.City;
        Context.Today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Ensure city has statistics enabled
        city.ReadOptions = city.ReadOptions.WithStatistics();

        // Load existing city from database or create new one
        var existingEntity = await dbContext.Cities
            .ApplyReadOptions(city.ReadOptions)
            .Include(c => c.AlternativeNames)
            .WhereCoordinates(city.Latitude, city.Longitude)
            .FirstOrDefaultAsync();

        Context.IsNew = existingEntity == null;

        // Apply update to existing entity or create new one
        Context.CityEntity = cityMapper.ApplyUpdate(existingEntity, city);

        // Chapter: Assign alternative name to the city
        await ReadChapter<AssignAlternativeNameChapter>();

        // Chapter: Increment search count statistics
        await ReadChapter<IncrementSearchCountChapter>();

        // Save the city entity to database
        if (Context.IsNew)
        {
            await dbContext.Cities.AddAsync(Context.CityEntity);
        }
        else
        {
            dbContext.Update(Context.CityEntity);
        }

        await dbContext.SaveChangesAsync();

        // Set the final result
        Context.Output.IsNew = Context.IsNew;
    }
}
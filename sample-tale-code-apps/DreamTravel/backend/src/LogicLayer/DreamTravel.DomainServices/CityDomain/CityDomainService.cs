using DreamTravel.DomainServices.CityDomain.SaveSteps;
using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.GeolocationDataClients.GoogleApi;
using DreamTravel.Trips.Sql;
using DreamTravel.Trips.Sql.QueryBuilders;
using Microsoft.EntityFrameworkCore;

namespace DreamTravel.DomainServices.CityDomain;

public interface ICityDomainService
{
    Task<City> Get(string name, Action<CityReadOptions>? configureOptions = null);
    Task<City> Get(double latitude, double longitude, Action<CityReadOptions>? configureOptions = null);
    Task Save(City city);
}

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
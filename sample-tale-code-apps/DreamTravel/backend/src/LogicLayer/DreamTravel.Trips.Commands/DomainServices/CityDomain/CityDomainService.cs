using DreamTravel.Trips.Commands.DomainServices.CityDomain.SaveSteps;
using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Sql;
using DreamTravel.Trips.Sql.DbModels;
using DreamTravel.Trips.Sql.QueryBuilders;
using Microsoft.EntityFrameworkCore;

namespace DreamTravel.Trips.Commands.DomainServices.CityDomain;

public interface ICityDomainService
{
    Task<City> Get(string name, CityReadOptions? options = null);
    Task<City> Get(double latitude, double longitude, CityReadOptions? options = null);
    Task Save(City city);
}


public class CityDomainService(
    ICityMapper cityMapper,
    IAssignAlternativeNameStep assignAlternativeNameStep,
    IIncrementSearchCountStep incrementSearchCountStep,
    DreamTripsDbContext dbContext) : ICityDomainService
{
    public async Task<City> Get(string name, CityReadOptions? options = null)
    {
        var cityEntity = await dbContext.Cities
            .ApplyReadOptions(options)
            .Include(c => c.AlternativeNames)
            .WhereName(name)
            .FirstOrDefaultAsync();

        if (cityEntity == null) return null;

        return cityMapper.ToDomain(cityEntity, name);
    }
    
    public async Task<City> Get(double latitude, double longitude, CityReadOptions? options = null)
    {
        var cityEntity = await dbContext.Cities
            .ApplyReadOptions(options)
            .Include(c => c.AlternativeNames)
            .WhereCoordinates(latitude, longitude)
            .FirstOrDefaultAsync();

        if (cityEntity == null) return null;

        return cityMapper.ToDomain(cityEntity, "name");
    }

    public async Task Save(City city)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        city.ReadOptions = city.ReadOptions.WithStatistics();

        var cityEntity = await dbContext.Cities
            .ApplyReadOptions(city.ReadOptions)
            .Include(c => c.AlternativeNames)
            .WhereCoordinates(city.Latitude, city.Longitude)
            .FirstOrDefaultAsync();
        
        cityMapper.ApplyUpdate(cityEntity, city);

        assignAlternativeNameStep.Invoke(cityEntity!, city.Name);
        incrementSearchCountStep.Invoke(cityEntity!, today);

        await dbContext.Cities.AddAsync(cityEntity!);
        
        //this is the only place where Save is called - to ensure no partial updates
        await dbContext.SaveChangesAsync();
    }
}
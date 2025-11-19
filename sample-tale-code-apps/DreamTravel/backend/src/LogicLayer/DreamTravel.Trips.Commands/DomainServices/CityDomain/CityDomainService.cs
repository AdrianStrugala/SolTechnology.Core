using DreamTravel.Trips.Commands.DomainServices.CityDomain.SaveSteps;
using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Sql;
using DreamTravel.Trips.Sql.DbModels;
using Microsoft.EntityFrameworkCore;

namespace DreamTravel.Trips.Commands.DomainServices.CityDomain;

public interface ICityDomainService
{
    Task<City?> Get(string name);
    Task Save(City city);
}


public class CityDomainService(
    ICityMapper cityMapper,
    IAssignAlternativeNameStep assignAlternativeNameStep,
    IIncrementSearchCountStep incrementSearchCountStep,
    DreamTripsDbContext dbContext) : ICityDomainService
{
    public async Task<City?> Get(string name)
    {
        var cityEntity = await dbContext.Cities
            .Include(c => c.AlternativeNames)
            .FirstOrDefaultAsync(c =>
                c.AlternativeNames.Any(an =>
                    an.AlternativeName.Equals(name, StringComparison.OrdinalIgnoreCase)));

        if (cityEntity == null) return null;

        return cityMapper.ToDomain(cityEntity, name);
    }

    public async Task Save(City city)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var cityEntity = await GetOrCreateCityAsync(city);

        assignAlternativeNameStep.Invoke(cityEntity, city.Name);
        incrementSearchCountStep.Invoke(cityEntity, today);

        //this is the only place where Save is called - to ensure no partial updates
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// 1) Pobierz city z bazy danych po koordynatach (podobne miasto),
    ///    albo utwórz nowe i dodaj do DbContextu (bez SaveChanges).
    /// </summary>
    private async Task<CityEntity> GetOrCreateCityAsync(City city)
    {
        var cityEntity = await dbContext.Cities
            .Include(c => c.AlternativeNames)
            .FirstOrDefaultAsync(c =>
                Math.Abs(c.Latitude - city.Latitude) < 0.001 &&  // ~100m
                Math.Abs(c.Longitude - city.Longitude) < 0.001);

        if (cityEntity != null)
            return cityEntity;

        // Tworzymy nowe miasto
        cityEntity = new CityEntity
        {
            CityId = Guid.NewGuid(),             // biznesowe Id
            Latitude = city.Latitude,
            Longitude = city.Longitude,
            Country = null,
            Region = null,
            Population = null
        };

        // jeśli chcesz użyć mappera:
        // cityMapper.ApplyUpdate(cityEntity, city);

        await dbContext.Cities.AddAsync(cityEntity);

        return cityEntity;
    }
}
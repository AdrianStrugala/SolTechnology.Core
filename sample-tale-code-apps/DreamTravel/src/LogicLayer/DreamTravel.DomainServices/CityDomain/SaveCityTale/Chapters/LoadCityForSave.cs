using DreamTravel.Domain.Cities;
using DreamTravel.Sql;
using DreamTravel.Sql.QueryBuilders;
using Microsoft.EntityFrameworkCore;
using SolTechnology.Core;
using SolTechnology.Core.Tale;

namespace DreamTravel.DomainServices.CityDomain.SaveCityTale.Chapters;

/// <summary>
/// Opening chapter of the save-city story. Loads the existing city entity (if any) for the given
/// coordinates, decides whether this is a create or an update, and projects the incoming city onto
/// the entity to persist.
/// </summary>
public class LoadCityForSave(DreamTripsDbContext dbContext, ICityMapper cityMapper)
    : Chapter<SaveCityContext>
{
    public override async Task<Result> Read(SaveCityContext context)
    {
        var city = context.Input.City;
        context.Today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Ensure city has statistics enabled
        city.ReadOptions = city.ReadOptions.WithStatistics();

        var existingEntity = await dbContext.Cities
            .ApplyReadOptions(city.ReadOptions)
            .Include(c => c.AlternativeNames)
            .WhereCoordinates(city.Latitude, city.Longitude)
            .FirstOrDefaultAsync();

        context.IsNew = existingEntity == null;
        context.CityEntity = cityMapper.ApplyUpdate(existingEntity, city);

        return Result.Success();
    }
}


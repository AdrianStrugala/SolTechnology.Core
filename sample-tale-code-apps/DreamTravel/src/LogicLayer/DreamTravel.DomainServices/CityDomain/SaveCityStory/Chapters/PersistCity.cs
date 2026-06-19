using DreamTravel.Sql;
using Microsoft.EntityFrameworkCore;
using SolTechnology.Core;
using SolTechnology.Core.Story;

namespace DreamTravel.DomainServices.CityDomain.SaveCityStory.Chapters;

/// <summary>
/// Closing chapter of the save-city story. Persists the prepared city entity — inserting a new row
/// or updating the existing one — and records on the output whether a city was created.
/// </summary>
public class PersistCity(DreamTripsDbContext dbContext) : Chapter<SaveCityContext>
{
    public override async Task<Result> Read(SaveCityContext context)
    {
        if (context.IsNew)
        {
            await dbContext.Cities.AddAsync(context.CityEntity);
        }
        else
        {
            dbContext.Update(context.CityEntity);
        }

        await dbContext.SaveChangesAsync();

        context.Output.IsNew = context.IsNew;

        return Result.Success();
    }
}


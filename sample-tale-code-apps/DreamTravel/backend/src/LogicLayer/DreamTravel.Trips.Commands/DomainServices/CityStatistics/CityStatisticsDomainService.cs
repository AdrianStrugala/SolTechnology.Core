using DreamTravel.Trips.Sql;
using DreamTravel.Trips.Sql.DbModels;
using Microsoft.EntityFrameworkCore;

namespace DreamTravel.Trips.Commands.DomainServices.CityStatistics;

public interface ICityStatisticsDomainService
{
    Task<CityStatisticsEntity> GetOrAdd(long cityId);
}

public class CityStatisticsDomainService(DreamTripsDbContext dbContext) : ICityStatisticsDomainService
{
    public async Task<CityStatisticsEntity> GetOrAdd(long cityId)
    {
        var stats = await dbContext.CityStatistics
            .SingleOrDefaultAsync(x => x.CityId == cityId);

        if (stats == null)
        {
            stats = new CityStatisticsEntity
            {
                CityId = cityId,
                SearchCount = 0
            };
            dbContext.CityStatistics.Add(stats);
            await dbContext.SaveChangesAsync();
        }

        return stats;
    }
}
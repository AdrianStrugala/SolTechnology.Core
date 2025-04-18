using DreamTravel.Trips.Domain.SearchStatistics;
using DreamTravel.Trips.Sql.DbModels;
using Microsoft.EntityFrameworkCore;

namespace DreamTravel.Trips.Sql.Repositories;

public interface ICityStatisticsRepository
{
    Task<List<CityStatistics>> GetAll();
    Task<CityStatisticsDbModel> GetOrAdd(long cityId);
}

public class CityStatisticsRepository(DreamTripsDbContext dbContext) : ICityStatisticsRepository
{
    public async Task<List<CityStatistics>> GetAll()
    {
        var result =
            from stats in dbContext.CityStatistics
            join city in dbContext.Cities on stats.CityId equals city.Id
            select new CityStatistics
            {
                CityName = city.Name,
                Country = city.Country,
                SearchCount = stats.SearchCount
            };

        return await result.ToListAsync();
    }
    
    public async Task<CityStatisticsDbModel> GetOrAdd(long cityId)
    {
        var stats = await dbContext.CityStatistics
            .SingleOrDefaultAsync(x => x.CityId == cityId);

        if (stats == null)
        {
            stats = new CityStatisticsDbModel
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
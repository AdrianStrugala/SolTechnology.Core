using DreamTravel.Trips.Domain.SearchStatistics;
using DreamTravel.Trips.Sql.DbModels;
using Microsoft.EntityFrameworkCore;

namespace DreamTravel.Trips.Sql.Repositories;

public interface ICountryStatisticsRepository
{
    Task<List<CountryStatisticsDbModel>> GetAll();
}

public class CountryStatisticsRepository(DreamTripsDbContext dbContext) : ICountryStatisticsRepository
{
    public async Task<List<CountryStatisticsDbModel>> GetAll()
    {
        return await dbContext.CountryStatistics.ToListAsync();
    }
}
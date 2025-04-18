using DreamTravel.Trips.Domain.Cities;

namespace DreamTravel.Trips.Sql.Repositories
{
    public interface ICityRepository
    {
        Task Add(CityDetails city);
    }

    public class CityRepository : ICityRepository
    {
        private readonly DreamTripsDbContext _dbContext;

        public CityRepository(DreamTripsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Add(CityDetails city)
        {
            await _dbContext.AddAsync(city);
            await _dbContext.SaveChangesAsync();
        }
    }
}

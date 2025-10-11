using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Sql;

namespace DreamTravel.Trips.Commands.DomainServices
{
    public interface ICityDomainService
    {
        Task Add(CityDetails city);
    }

    public class CityDomainService(DreamTripsDbContext dbContext) : ICityDomainService
    {
        //The domain service is meant to be the single point of domain entity modification
        //This is to ensure, that the Add/Update/Delete behavior remains consistent across the application
        //It could perform additional validation and trigger side effects like sending notifications, creating change tracking
        
        public async Task Add(CityDetails city)
        {
            await dbContext.AddAsync(city);
            await dbContext.SaveChangesAsync();
        }
    }
}

using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Sql;

namespace DreamTravel.Trips.Commands.DomainServices
{
    public interface ICityDomainService
    {
        Task Add(City city);
    }

    public class CityDomainService(DreamTripsDbContext dbContext) : ICityDomainService
    {
        //TODO: add CityExtendedBuilder for CityDetails
        
        //The domain service is meant to be the single point of domain entity modification
        //This is to ensure, that the Add/Update/Delete behavior remains consistent across the application
        //It could perform additional validation and trigger side effects like sending notifications, creating change tracking
        
        public async Task Add(City city)
        {
            //TODO: on add ensure that City with the same coordinates
            // is not already present in the database
            // if so, save the requested as an alternative name
            
            await dbContext.AddAsync(city);
            await dbContext.SaveChangesAsync();
        }
    }
}

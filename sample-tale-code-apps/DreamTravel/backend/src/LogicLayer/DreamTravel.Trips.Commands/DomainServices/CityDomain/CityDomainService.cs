using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Sql;
using DreamTravel.Trips.Sql.DbModels;
using Microsoft.EntityFrameworkCore;

namespace DreamTravel.Trips.Commands.DomainServices.CityDomain
{
    public interface ICityDomainService
    {
        Task Add(City city);
    }

    public class CityDomainService(
        ICityMapper cityMapper,
        DreamTripsDbContext dbContext) : ICityDomainService
    {
        //TODO: add CityExtendedBuilder for CityDetails
        
        //The domain service is meant to be the single point of domain entity modification
        //This is to ensure, that the Add/Update/Delete behavior remains consistent across the application
        //It could perform additional validation and trigger side effects like sending notifications, creating change tracking
        
        public async Task Add(City city)
        {
            // Check if city with same coordinates already exists
            var cityEntity = await dbContext.Cities
                .Include(c => c.AlternativeNames)
                .FirstOrDefaultAsync(c => 
                    Math.Abs(c.Latitude - city.Latitude) < 0.001 && //100m approximation
                    Math.Abs(c.Longitude - city.Longitude) < 0.001);
    
            if (cityEntity != null)
            {
                // City already exists - add the new name as an alternative if not already present
                if (!cityEntity.AlternativeNames.Any(an => 
                        an.AlternativeName.Equals(city.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    cityEntity.AlternativeNames.Add(new AlternativeNameEntity
                    {
                        CityId = cityEntity.Id,
                        AlternativeName = city.Name
                    });
                }
        
                return;
            }
            else
            {
                cityMapper.ApplyUpdate(cityEntity, city);;
                await dbContext.Cities.AddAsync(cityEntity!);
            }
    
            await dbContext.SaveChangesAsync();
        }
    }
}

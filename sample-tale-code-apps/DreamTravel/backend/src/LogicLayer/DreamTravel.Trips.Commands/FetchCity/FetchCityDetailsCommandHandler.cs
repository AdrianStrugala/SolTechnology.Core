using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.GeolocationDataClients.GeoDb;
using DreamTravel.Trips.Sql.Repositories;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Trips.Commands.FetchCity
{
    public class FetchCityDetailsCommandHandler(
        ICityRepository cityRepository,
        IGeoDbApiClient geoDbApiClient,
        ICityStatisticsRepository cityStatisticsRepository,
        ILogger<FetchCityDetailsCommandHandler> logger)
        : ICommandHandler<FetchCityDetailsCommand>
    {
        public async Task<Result> Handle(FetchCityDetailsCommand request, CancellationToken cancellationToken)
        {
            CityDetails cityDetails;
            try
            {
                var geoDbResponse = await geoDbApiClient.GetCityDetails(request.Name);
                if (geoDbResponse == null)
                {
                    logger.LogWarning($"Failed to fetch data about city: [{request.Name}]");
                    return Result.Fail("Failed to fetch data about city: [{request.Name}]");
                }

                cityDetails = new CityDetails
                {
                    Country = geoDbResponse.Country,
                    Latitude = geoDbResponse.Latitude,
                    Longitude = geoDbResponse.Longitude,
                    Name = request.Name,
                    Population = geoDbResponse.Population,
                    Region = geoDbResponse.Region
                };

                await cityRepository.Add(cityDetails);
                
                
            }
            catch (Exception e)
            {
                logger.Log(LogLevel.Error, e, "Failed to fetch data about city: [{request.Name}]");
                throw;
            }

            await BumpSearchStatistics(cityDetails.Id);

            return Result.Success();
        }

        private async Task BumpSearchStatistics(long cityDetailsId)
        {
            var cityStatistics = await cityStatisticsRepository.GetOrAdd(cityDetailsId);
            cityStatistics.SearchCount++;
        }
    }
}

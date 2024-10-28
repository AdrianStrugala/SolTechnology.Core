using DreamTravel.GeolocationData.GeoDb;
using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Sql.Repositories;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Trips.Commands.FetchCity
{
    public class FetchCityDetailsCommandHandler : ICommandHandler<FetchCityDetailsCommand>
    {
        private readonly ICityRepository _cityRepository;
        private readonly IGeoDbApiClient _geoDbApiClient;
        private readonly ILogger<FetchCityDetailsCommandHandler> _logger;

        public FetchCityDetailsCommandHandler(ICityRepository cityRepository, IGeoDbApiClient geoDbApiClient, ILogger<FetchCityDetailsCommandHandler> logger)
        {
            _cityRepository = cityRepository;
            _geoDbApiClient = geoDbApiClient;
            _logger = logger;
        }

        public async Task<Result> Handle(FetchCityDetailsCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var geoDbResponse = await _geoDbApiClient.GetCityDetails(request.Name);
                if (geoDbResponse == null)
                {
                    _logger.LogWarning($"Failed to fetch data about city: [{request.Name}]");
                    return Result.Fail("Failed to fetch data about city: [{request.Name}]");
                }
                var cityDetails = new CityDetails
                {
                    Country = geoDbResponse.Country,
                    Latitude = geoDbResponse.Latitude,
                    Longitude = geoDbResponse.Longitude,
                    Name = request.Name,
                    Population = geoDbResponse.Population,
                    Region = geoDbResponse.Region
                };

                await _cityRepository.Add(cityDetails);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return Result.Success();
        }
    }
}

using DreamTravel.GeolocationData.GeoDb;
using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Sql.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DreamTravel.Trips.Commands.FetchCity
{
    public class FetchCityDetailsCommandHandler : IRequestHandler<FetchCityDetailsCommand>
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

        public async Task Handle(FetchCityDetailsCommand request, CancellationToken cancellationToken)
        {
            var geoDbResponse = await _geoDbApiClient.GetCityDetails(request.Name);
            if (geoDbResponse == null)
            {
                _logger.LogWarning($"Failed to fetch data about city: [{request.Name}]");
                return;
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
    }
}

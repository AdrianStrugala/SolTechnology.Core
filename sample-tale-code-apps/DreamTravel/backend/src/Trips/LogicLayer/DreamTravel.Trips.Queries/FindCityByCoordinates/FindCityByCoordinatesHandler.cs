using DreamTravel.GeolocationData.GoogleApi;
using DreamTravel.Trips.Domain.Cities;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Trips.Queries.FindCityByCoordinates
{
    public class FindCityByCoordinatesHandler : IQueryHandler<FindCityByCoordinatesQuery, City>
    {
        private readonly IGoogleApiClient _googleApiClient;

        public FindCityByCoordinatesHandler(IGoogleApiClient googleApiClient)
        {
            _googleApiClient = googleApiClient;
        }

        public async Task<Result<City>> Handle(FindCityByCoordinatesQuery query, CancellationToken cancellationToken)
        {
            City result = new City
            {
                Latitude = query.Lat,
                Longitude = query.Lng
            };

            result = await _googleApiClient.GetNameOfCity(result);

            return result;
        }
    }
}

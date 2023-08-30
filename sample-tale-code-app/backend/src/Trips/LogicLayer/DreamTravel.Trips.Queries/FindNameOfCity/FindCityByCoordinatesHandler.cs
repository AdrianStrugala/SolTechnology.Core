using DreamTravel.GeolocationData;
using DreamTravel.Infrastructure;
using DreamTravel.Trips.Domain.Cities;

namespace DreamTravel.Trips.Queries.FindNameOfCity
{
    public class FindCityByCoordinatesHandler : IQueryHandler<FindCityByCoordinatesQuery, City>
    {
        private readonly IGoogleApiClient _googleApiClient;

        public FindCityByCoordinatesHandler(IGoogleApiClient googleApiClient)
        {
            _googleApiClient = googleApiClient;
        }

        public async Task<City> Handle(FindCityByCoordinatesQuery byCoordinatesQuery)
        {
            City result = new City
            {
                Latitude = byCoordinatesQuery.Lat,
                Longitude = byCoordinatesQuery.Lng
            };

            result = await _googleApiClient.GetNameOfCity(result);

            return result;
        }
    }
}

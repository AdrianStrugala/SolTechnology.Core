using System.Threading.Tasks;
using DreamTravel.Domain.Cities;
using DreamTravel.GeolocationData;
using DreamTravel.Infrastructure;

namespace DreamTravel.DreamTrips.FindNameOfCity
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

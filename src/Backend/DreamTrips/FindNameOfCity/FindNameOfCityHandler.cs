using System.Threading.Tasks;
using DreamTravel.Domain.Cities;
using DreamTravel.GeolocationData;

namespace DreamTravel.DreamTrips.FindNameOfCity
{
    public class FindNameOfCityHandler : IFindNameOfCity
    {
        private readonly IGoogleApiClient _googleApiClient;

        public FindNameOfCityHandler(IGoogleApiClient googleApiClient)
        {
            _googleApiClient = googleApiClient;
        }

        public async Task<City> Handle(double lat, double lng)
        {
            City result = new City
            {
                Latitude = lat,
                Longitude = lng
            };

            result = await _googleApiClient.GetNameOfCity(result);

            return result;
        }
    }
}

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

        public async Task<City> Handle(FindNameOfCityQuery query)
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

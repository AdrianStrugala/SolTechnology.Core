using System.Threading.Tasks;
using DreamTravel.Domain.Cities;
using DreamTravel.GeolocationData;

namespace DreamTravel.DreamTrips.FindLocationOfCity
{
    public class FindLocationOfCityHandler : IFindLocationOfCity
    {
        private readonly IGoogleApiClient _googleApiClient;

        public FindLocationOfCityHandler(IGoogleApiClient googleApiClient)
        {
            _googleApiClient = googleApiClient;
        }

        public async Task<City> Handle(string cityName)
        {
            var result = await _googleApiClient.GetLocationOfCity(cityName);

            return result;
        }
    }
}

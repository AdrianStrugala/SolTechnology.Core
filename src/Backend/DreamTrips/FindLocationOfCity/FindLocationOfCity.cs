using System.Threading.Tasks;
using DreamTravel.Domain.Cities;
using DreamTravel.GeolocationData;

namespace DreamTravel.DreamTrips.FindLocationOfCity
{
    public class FindLocationOfCity : IFindLocationOfCity
    {
        private readonly IGoogleApiClient _googleApiClient;

        public FindLocationOfCity(IGoogleApiClient googleApiClient)
        {
            _googleApiClient = googleApiClient;
        }

        public async Task<City> Execute(string cityName)
        {
            var result = await _googleApiClient.GetLocationOfCity(cityName);

            return result;
        }
    }
}

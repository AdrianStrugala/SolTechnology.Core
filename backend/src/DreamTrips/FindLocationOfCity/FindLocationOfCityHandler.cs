using System.Threading.Tasks;
using DreamTravel.Domain.Cities;
using DreamTravel.GeolocationData;
using DreamTravel.Infrastructure;

namespace DreamTravel.DreamTrips.FindLocationOfCity
{
    public class FindLocationOfCityHandler : IQueryHandler<FindLocationOfCityQuery, City>
    {
        private readonly IGoogleApiClient _googleApiClient;

        public FindLocationOfCityHandler(IGoogleApiClient googleApiClient)
        {
            _googleApiClient = googleApiClient;
        }

        public async Task<City> Handle(FindLocationOfCityQuery query)
        {
            var result = await _googleApiClient.GetLocationOfCity(query.Name);

            return result;
        }
    }
}

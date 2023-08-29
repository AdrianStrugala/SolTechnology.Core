using System.Threading.Tasks;
using DreamTravel.GeolocationData;
using DreamTravel.Infrastructure;
using DreamTravel.Trips.Domain.Cities;

namespace DreamTravel.DreamTrips.FindLocationOfCity
{
    public class FindCityByNameHandler : IQueryHandler<FindCityByNameQuery, City>
    {
        private readonly IGoogleApiClient _googleApiClient;

        public FindCityByNameHandler(IGoogleApiClient googleApiClient)
        {
            _googleApiClient = googleApiClient;
        }

        public async Task<City> Handle(FindCityByNameQuery byNameQuery)
        {
            var result = await _googleApiClient.GetLocationOfCity(byNameQuery.Name);

            return result;
        }
    }
}

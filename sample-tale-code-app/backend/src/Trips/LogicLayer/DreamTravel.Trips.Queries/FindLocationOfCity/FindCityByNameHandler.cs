using DreamTravel.GeolocationData.GoogleApi;
using DreamTravel.Infrastructure;
using DreamTravel.Trips.Domain.Cities;

namespace DreamTravel.Trips.Queries.FindLocationOfCity
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

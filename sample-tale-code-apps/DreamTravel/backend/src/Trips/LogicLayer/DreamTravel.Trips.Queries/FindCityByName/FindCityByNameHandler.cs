using DreamTravel.GeolocationData.GoogleApi;
using DreamTravel.Trips.Domain.Cities;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Trips.Queries.FindCityByName
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

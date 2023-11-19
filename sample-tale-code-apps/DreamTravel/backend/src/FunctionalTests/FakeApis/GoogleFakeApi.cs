using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.GeolocationData.GoogleApi;
using DreamTravel.Trips.Domain.Cities;
using SolTechnology.Core.Faker.FakesBase;
using WireMock.Matchers;
using WireMock.RequestBuilders;

namespace DreamTravel.FunctionalTests.FakeApis
{
    public class GoogleFakeApi : FakeService<IGoogleApiClient>, IFakeApi, IGoogleApiClient
    {
        protected override string BaseUrl => "google";


        // /// <summary>
        // /// The convention for Fake API methods:
        // /// 1. Use the same name as for Client methods
        // /// 2. Return RequestInfo model containing http method and path used in the call
        // /// 3. The methods here have no reference, because are called dynamically
        // /// </summary>
        //
        // public RequestInfo GetLocationOfCity()
        // {
        //     return new RequestInfo(HttpMethod.Get, "maps/api/geocode/json?address={cityName}&key={key}");
        // }

        public Task<City> GetLocationOfCity(string cityName)
        {
            var request = Request
                .Create()
                .UsingGet()
                .WithPath(new WildcardMatcher($"/{BaseUrl}/maps/api/geocode/json"))
                .WithParam("address", $"{cityName}")
                .WithParam("key", "googleKey");

            Provider = BuildRequest(request);

            return null!;
        }

        public Task<double[]> GetDurationMatrixByTollRoad(List<City> listOfCities)
        {
            throw new System.NotImplementedException();
        }

        public Task<double[]> GetDurationMatrixByFreeRoad(List<City> listOfCities)
        {
            throw new System.NotImplementedException();
        }


        public Task<City> GetNameOfCity(City city)
        {
            throw new System.NotImplementedException();
        }
    }
}
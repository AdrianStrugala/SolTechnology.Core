using System.Net.Http;
using DreamTravel.GeolocationData.GoogleApi;
using SolTechnology.Core.Faker.FakesBase;
using SolTechnology.Core.Faker.WireMock;
using WireMock.Server;

namespace DreamTravel.FunctionalTests.FakeApis
{
    public class GoogleFakeApi : FakeService<IGoogleApiClient>, IFakeApi
    {
        protected override IWireMockFakerConfigurator<IGoogleApiClient> Configure(WireMockServer mockServer) =>
            mockServer
                .CreateFor<IGoogleApiClient>() 
                .WithBaseUrl("google");


        /// <summary>
        /// The convention for Fake API methods:
        /// 1. Use the same name as for Client methods
        /// 2. Return RequestInfo model containing http method and path used in the call
        /// 3. The methods here have no reference, because are called dynamically
        /// </summary>

        public RequestInfo GetLocationOfCity()
        {
            return new RequestInfo(HttpMethod.Get, "maps/api/geocode/json?address={cityName}&key={key}");
        }
    }
}

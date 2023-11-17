using System.Net.Http;
using SolTechnology.TaleCode.ApiClients.ApiFootballApi;
using TaleCode.Faker.FakesBase;
using TaleCode.Faker.WireMock;
using WireMock.Server;

namespace TaleCode.FunctionalTests.FakeApis
{
    public class ApiFootballFakeApi : FakeService<IApiFootballApiClient>, IFakeApi
    {
        protected override IWireMockFakerConfigurator<IApiFootballApiClient> Configure(WireMockServer mockServer) =>
            mockServer
                .CreateFor<IApiFootballApiClient>() 
                .WithBaseUrl("api-football");


        /// <summary>
        /// The convention for Fake API methods:
        /// 1. Use the same name as for Client methods
        /// 2. Return RequestInfo model containing http method and path used in the call
        /// 3. The methods here have no reference, because are called dynamically
        /// </summary>

        public RequestInfo GetPlayerTeams()
        {
            return new RequestInfo(HttpMethod.Get, "v3/transfers");
        }
    }
}

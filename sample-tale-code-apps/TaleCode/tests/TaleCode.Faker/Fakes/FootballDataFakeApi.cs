using SolTechnology.TaleCode.ApiClients.FootballDataApi;
using TaleCode.Faker.FakesBase;
using TaleCode.Faker.WireMock;
using WireMock.Server;

namespace TaleCode.Faker.Fakes
{
    public class FootballDataFakeApi : FakeService<IFootballDataApiClient>, IFakeApi
    {
        protected override IWireMockFakerConfigurator<IFootballDataApiClient> Configure(WireMockServer mockServer) =>
            mockServer
                .CreateFor<IFootballDataApiClient>()
                .WithBaseUrl("football-data");


        public RequestInfo GetPlayerById()
        {
            return new RequestInfo(
                HttpMethod.Get,
                "v2/players/{id}/matches",
                new Dictionary<string, string> { { "limit", "999" } });
        }

        public RequestInfo GetMatchById()
        {
            return new RequestInfo(HttpMethod.Get, "v2/matches/{matchId}");
        }
    }
}

using System.Threading.Tasks;
using SolTechnology.Core.Faker.FakesBase;
using SolTechnology.TaleCode.ApiClients.FootballDataApi;
using SolTechnology.TaleCode.ApiClients.FootballDataApi.Models;
using WireMock.Matchers;
using WireMock.RequestBuilders;

namespace TaleCode.FunctionalTests.FakeApis
{
    public class FootballDataFakeApi : FakeService<IFootballDataApiClient>, IFakeApi, IFootballDataApiClient
    {
        protected override string BaseUrl => "football-data";

        public Task<FootballDataPlayer> GetPlayerById(int id)
        {
            var request = Request
                .Create()
                .UsingGet()
                .WithPath(new WildcardMatcher($"/{BaseUrl}/v2/players/{id}/matches"))
                .WithParam("limit", "999");

            Provider = BuildRequest(request);

            return default;
        }

        public Task<FootballDataMatch> GetMatchById(int matchApiId)
        {
            var request = Request
                .Create()
                .UsingGet()
                .WithPath(new WildcardMatcher($"/{BaseUrl}/v2/matches/{matchApiId}"));

            Provider = BuildRequest(request);

            return default;
        }
    }
}

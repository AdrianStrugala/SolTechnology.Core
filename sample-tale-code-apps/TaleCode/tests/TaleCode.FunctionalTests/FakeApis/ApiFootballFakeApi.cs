using System.Collections.Generic;
using System.Threading.Tasks;
using SolTechnology.Core.Faker.FakesBase;
using SolTechnology.TaleCode.ApiClients.ApiFootballApi;
using SolTechnology.TaleCode.ApiClients.ApiFootballApi.Models;
using WireMock.Matchers;
using WireMock.RequestBuilders;

namespace TaleCode.FunctionalTests.FakeApis
{
    public class ApiFootballFakeApi : FakeApiBase<IApiFootballApiClient>, IApiFootballApiClient
    {
        protected override string BaseUrl => "api-football";

        public Task<List<Team>> GetPlayerTeams(int apiId)
        {
            var request = Request
                .Create()
                .UsingGet()
                .WithPath(new WildcardMatcher($"/{BaseUrl}/v3/transfers"));

            Provider = BuildRequest(request);

            return default;
        }
    }
}

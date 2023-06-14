using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using SolTechnology.TaleCode.ApiClients.ApiFootballApi;
using SolTechnology.TaleCode.ApiClients.ApiFootballApi.Models;
using SolTechnology.TaleCode.ApiClients.FootballDataApi;
using SolTechnology.TaleCode.ApiClients.FootballDataApi.Models;
using SolTechnology.TaleCode.BackgroundWorker.InternalApi;
using TaleCode.FunctionalTests.TestsConfiguration;
using TaleCode.IntegrationTests.SqlData;
using Xunit;

namespace TaleCode.FunctionalTests
{
    [Collection(nameof(TaleCodeFunctionalTestsCollection))]
    public class SynchronizationTest
    {
        private readonly SqlFixture _sqlFixture;
        private readonly HttpClient _backgroundWorkerClient;
        private readonly WireMockFixture _wireMockFixture;
        private readonly TestServer _backgroundWorker;

        public SynchronizationTest(FunctionalTestsFixture functionalTestsFixture)
        {
            _backgroundWorkerClient = functionalTestsFixture.BackgroundWorkerFixture.ServerClient;
            _backgroundWorker = functionalTestsFixture.BackgroundWorkerFixture.TestServer;
            _sqlFixture = functionalTestsFixture.SqlFixture;
            _wireMockFixture = functionalTestsFixture.WireMockFixture;
        }

        [Theory, AutoFixtureData]
        [EndpointReference(nameof(SynchronizePlayerMatchesController), nameof(SynchronizePlayerMatchesController.SynchronizePlayerMatches))]
        public async Task After_Synchronization_Player_Data_Can_Be_Accessed_By_Api(
            PlayerModel playerResponse,
            TransferDetails transfersResponse,
            List<MatchModel> matchesResponse)

        {
            //Arrange
            int playerId = 44;
            int apiFootballPlayerId = 874;
            playerResponse.Player.Id = playerId;

            _wireMockFixture.Fake<IFootballDataApiClient>()
                .WithRequest(x => x.GetPlayerById, new Dictionary<string, string> { { "id", playerId.ToString() } })
                .WithResponse(x => x.WithSuccess().WithBodyAsJson(playerResponse));

            for (int i = 0; i < playerResponse.Matches.Count; i++)
            {
                matchesResponse[i].Match = playerResponse.Matches[i];

                _wireMockFixture.Fake<IFootballDataApiClient>()
                    .WithRequest(x => x.GetMatchById, new Dictionary<string, string> { { "matchId", matchesResponse[i].Match.Id.ToString() } })
                    .WithResponse(x => x.WithSuccess().WithBodyAsJson(matchesResponse[i]));
            }

            _wireMockFixture.Fake<IApiFootballApiClient>()
                .WithRequest(x => x.GetPlayerTeams, null, new Dictionary<string, string> { { "player", apiFootballPlayerId.ToString() } })
                .WithResponse(x => x.WithSuccess().WithBodyAsJson(transfersResponse));


            //Act
            var synchronizationResponse = await _backgroundWorker
                .CreateRequest($"api/SynchronizePlayerMatches/{playerId}")
                .GetAsync();

            await Task.Delay(60000);

            synchronizationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
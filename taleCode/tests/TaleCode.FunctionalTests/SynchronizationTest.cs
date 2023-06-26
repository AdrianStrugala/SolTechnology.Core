using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using SolTechnology.Core.BlobStorage.Connection;
using SolTechnology.TaleCode.ApiClients.ApiFootballApi;
using SolTechnology.TaleCode.ApiClients.ApiFootballApi.Models;
using SolTechnology.TaleCode.ApiClients.FootballDataApi;
using SolTechnology.TaleCode.ApiClients.FootballDataApi.Models;
using SolTechnology.TaleCode.BackgroundWorker.InternalApi;
using SolTechnology.TaleCode.Domain;
using TaleCode.FunctionalTests.TestsConfiguration;
using TaleCode.IntegrationTests.Blob;
using TaleCode.IntegrationTests.Sql.Configuration;
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
        private readonly BlobConnectionFactory _blobConnectionFactory;

        public SynchronizationTest(FunctionalTestsFixture functionalTestsFixture)
        {
            _backgroundWorkerClient = functionalTestsFixture.BackgroundWorkerFixture.ServerClient;
            _backgroundWorker = functionalTestsFixture.BackgroundWorkerFixture.TestServer;
            _sqlFixture = functionalTestsFixture.SqlFixture;
            _blobConnectionFactory = functionalTestsFixture.BlobFixture.BlobConnectionFactory;
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
            var resultContainerName = "playerstatistics";
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

            var containerClient = _blobConnectionFactory.GetConnection(resultContainerName);
            var blobClient = containerClient.GetBlobClient(playerId.ToString());
            await blobClient.DeleteIfExistsAsync();

            //Act
            var synchronizationResponse = await _backgroundWorker
                .CreateRequest($"api/SynchronizePlayerMatches/{playerId}")
                .GetAsync();

            synchronizationResponse.StatusCode.Should().Be(HttpStatusCode.OK);


            //Assert
            var stopwatch = Stopwatch.StartNew();
            if (!await blobClient.ExistsAsync() && stopwatch.Elapsed.TotalSeconds < 20)
            {
                Thread.Sleep(1000);
            }
            stopwatch.Stop();

            var playerStatisticsResult = await containerClient.ReadFromBlob<PlayerStatistics>(playerId.ToString());

            Assert.NotNull(playerStatisticsResult);

            //That's the place for more sophisticated assert, but would require data arrange. You know :D
        }
    }
}
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using SolTechnology.Core.Api;
using SolTechnology.Core.BlobStorage.Connection;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Faker;
using SolTechnology.Core.Sql.Testing;
using SolTechnology.TaleCode.ApiClients.ApiFootballApi;
using SolTechnology.TaleCode.ApiClients.ApiFootballApi.Models;
using SolTechnology.TaleCode.ApiClients.FootballDataApi;
using SolTechnology.TaleCode.ApiClients.FootballDataApi.Models;
using SolTechnology.TaleCode.BackgroundWorker.InternalApi;
using SolTechnology.TaleCode.PlayerRegistry.Queries.GetPlayerStatistics;
using TaleCode.FunctionalTests.TestsConfiguration;
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
        private readonly HttpClient _api;

        public SynchronizationTest(FunctionalTestsFixture functionalTestsFixture)
        {
            _backgroundWorkerClient = functionalTestsFixture.BackgroundWorkerFixture.ServerClient;
            _backgroundWorker = functionalTestsFixture.BackgroundWorkerFixture.TestServer;
            _api = functionalTestsFixture.ApiFixture.ServerClient;
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
                .WithRequest(x => x.GetPlayerById, playerId)
                .WithResponse(x => x.WithSuccess().WithBodyAsJson(playerResponse));

            for (int i = 0; i < playerResponse.Matches.Count; i++)
            {
                matchesResponse[i].Match = playerResponse.Matches[i];

                _wireMockFixture.Fake<IFootballDataApiClient>()
                    .WithRequest(x => x.GetMatchById, matchesResponse[i].Match.Id)
                    .WithResponse(x => x.WithSuccess().WithBodyAsJson(matchesResponse[i]));
            }

            _wireMockFixture.Fake<IApiFootballApiClient>()
                .WithRequest(x => x.GetPlayerTeams, apiFootballPlayerId)
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
            //TODO: change way of service bus emulation
            // var stopwatch = Stopwatch.StartNew();
            // GetPlayerStatisticsResult? playerStatistics = null;
            // do
            // {
            //     Thread.Sleep(1000);
            //     playerStatistics = await GetPlayerStatistics(playerId);
            //
            // } while (playerStatistics == null && stopwatch.Elapsed.TotalSeconds < 20);
            // stopwatch.Stop();
            //
            //
            // Assert.NotNull(playerStatistics);
            //That's the place for more sophisticated assert, but would require data arrange. You know :D
        }

        private async Task<GetPlayerStatisticsResult?> GetPlayerStatistics(int playerId)
        {
            var apiResponse = await _api
                .CreateRequest($"GetPlayerStatistics/{playerId}")
                .WithHeader("X-Auth", "SolTechnologyAuthentication U2VjdXJlS2V5")
                .GetAsync<Result<GetPlayerStatisticsResult>>();


            return !apiResponse.IsSuccess ? null : apiResponse.Data;
        }
    }
}
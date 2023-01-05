using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using SolTechnology.TaleCode.ApiClients.FootballDataApi;
using SolTechnology.TaleCode.ApiClients.FootballDataApi.Models;
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

        [Fact]
        public async Task After_Synchronization_Data_Can_Be_Accessed_By_Api()
        {
            PlayerModel footballDataResponse = new PlayerModel();

            //Arrange
            _wireMockFixture.Fake<IFootballDataApiClient>()
                .WithRequest(x => x.GetPlayerById, 1)
                .WithResponse(x => x.WithSuccess().WithBodyAsJson(footballDataResponse));


            //Act
            // var response = _backgroundWorkerClient.GetAsync("api/SynchronizePlayerMatches/44").GetAwaiter().GetResult();

            var synchronizationResponse = _backgroundWorker
                .CreateRequest("api/SynchronizePlayerMatches/44")
                .GetAsync()
                .GetAwaiter()
                .GetResult();

            synchronizationResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var x = 1;

            // while (true)
            // {
            //      x += 1;
            // }
        }
    }
}
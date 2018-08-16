using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using DreamTravel;
using DreamTravel.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace DreamTravelITests.Authentication
{
    public class DreamAuthenticationTests

    {
        private readonly HttpClient _httpServer;
        private const string TestUrl = "TSP/FindCity";
        public DreamAuthenticationTests()
        {
            var webHostBuilder = new WebHostBuilder()
                .UseStartup<Startup>();
            var testServer = new TestServer(webHostBuilder);
            _httpServer = testServer.CreateClient();
        }

        [Fact]
        public async Task HandleAuthenticateAsync_NoAuthenticationHeader_Unauthorized()
        {
            //Arrange

            //Act
            var result = await _httpServer.PostAsync(TestUrl, new StringContent(""));

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        }

        [Fact]
        public async Task HandleAuthenticateAsync_InvalidAuthenticationHeader_Unauthorized()
        {
            //Arrange
            _httpServer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("invalidHeader");

            //Act
            var result = await _httpServer.PostAsync(TestUrl, new StringContent(""));

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        }

        [Fact]
        public async Task HandleAuthenticateAsync_InvalidAuthenticationSchema_Unauthorized()
        {
            //Arrange
            _httpServer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("invalidSchema", "invalidKey");

            //Act
            var result = await _httpServer.PostAsync(TestUrl, new StringContent(""));

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        }

        [Fact]
        public async Task HandleAuthenticateAsync_InvalidAuthenticationKey_Unauthorized()
        {
            //Arrange
            _httpServer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(nameof(DreamAuthentication), "invalidKey");

            //Act
            var result = await _httpServer.PostAsync(TestUrl, new StringContent(""));

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        }

        [Fact]
        public async Task HandleAuthenticateAsync_ValidRequest_Authorized()
        {
            //Arrange
            _httpServer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(DreamAuthenticationOptions.AuthenticationScheme, "TestAuthentication");

            //Act
            var result = await _httpServer.PostAsync(TestUrl, new StringContent(""));


            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }
    }
}
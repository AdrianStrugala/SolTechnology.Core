using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using DreamTravel.ApiTests.TestsConfiguration;
using DreamTravel.Infrastructure.Authentication;
using Xunit;

namespace DreamTravel.ApiTests.Authentication
{
    public class DreamAuthenticationTests : IClassFixture<HttpClientFixture>
    {
        private readonly HttpClient _httpServer;
        private const string TestUrl = "";
        public DreamAuthenticationTests(HttpClientFixture fixture)
        {
            _httpServer = fixture.ServerClient;
        }

        [Fact]
        public async Task HandleAuthenticateAsync_NoAuthenticationHeader_Unauthorized()
        {
            //Arrange

            //Act
            var result = await _httpServer.GetAsync(TestUrl);

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        }

        [Fact]
        public async Task HandleAuthenticateAsync_InvalidAuthenticationHeader_Unauthorized()
        {
            //Arrange
            _httpServer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("invalidHeader");

            //Act
            var result = await _httpServer.GetAsync(TestUrl);

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        }

        [Fact]
        public async Task HandleAuthenticateAsync_InvalidAuthenticationSchema_Unauthorized()
        {
            //Arrange
            _httpServer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("invalidSchema", "invalidKey");

            //Act
            var result = await _httpServer.GetAsync(TestUrl);

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        }

        [Fact]
        public async Task HandleAuthenticateAsync_InvalidAuthenticationKey_Unauthorized()
        {
            //Arrange
            _httpServer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(nameof(DreamAuthentication), "invalidKey");

            //Act
            var result = await _httpServer.GetAsync(TestUrl);

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        }

        [Fact]
        public async Task HandleAuthenticateAsync_ValidRequest_Authorized()
        {
            //Arrange
            _httpServer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(DreamAuthenticationOptions.AuthenticationScheme, "U29sVWJlckFsbGVz");

            //Act
            var result = await _httpServer.GetAsync(TestUrl);

            //Assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        }
    }
}
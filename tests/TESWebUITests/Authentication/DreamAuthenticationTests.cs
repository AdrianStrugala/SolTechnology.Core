//using System;
//using System.Collections.Generic;
//using System.Net;
//using System.Net.Http;
//using System.Net.Http.Headers;
//using System.Text;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Hosting;
//using TESWebUI;
//using TESWebUI.Authentication;
//using Xunit;
//
//namespace TESWebUITests.Authentication
//{
//    public class DreamAuthenticationTests
//
//    {
//        private readonly HttpClient _httpServer;
//        private const string TestUrl = "api/kupa/result";
//        public DreamAuthenticationTests()
//        {
//            var webHostBuilder = new WebHostBuilder()
//                .UseStartup<Startup>();
//            var testServer = new TestServer(webHostBuilder);
//            _httpServer = testServer.CreateClient();
//        }
//
//        [Fact]
//        public async Task HandleAuthenticateAsync_NoAuthenticationHeader_Unauthorized()
//        {
//            //Arrange
//
//            //Act
//            var result = await _httpServer.GetAsync(TestUrl);
//
//            //Assert
//            Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
//        }
//
//        [Fact]
//        public async Task HandleAuthenticateAsync_InvalidAuthenticationHeader_Unauthorized()
//        {
//            //Arrange
//            _httpServer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("invalidHeader");
//
//            //Act
//            var result = await _httpServer.GetAsync(TestUrl);
//
//            //Assert
//            Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
//        }
//
//        [Fact]
//        public async Task HandleAuthenticateAsync_InvalidAuthenticationSchema_Unauthorized()
//        {
//            //Arrange
//            _httpServer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("invalidSchema", "invalidKey");
//
//            //Act
//            var result = await _httpServer.GetAsync(TestUrl);
//
//            //Assert
//            Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
//        }
//
//        [Fact]
//        public async Task HandleAuthenticateAsync_InvalidAuthenticationKey_Unauthorized()
//        {
//            //Arrange
//            _httpServer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(nameof(DreamAuthentication), "invalidKey");
//
//            //Act
//            var result = await _httpServer.GetAsync(TestUrl);
//
//            //Assert
//            Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
//        }
//
//        [Fact]
//        public async Task HandleAuthenticateAsync_ValidRequest_Authorized()
//        {
//            //Arrange
//            _httpServer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(DreamAuthenticationOptions.AuthenticationScheme, "???");
//
//            //Act
//            var result = await _httpServer.GetAsync(TestUrl);
//
//
//            //Assert
//            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
//        }
//    }
//}
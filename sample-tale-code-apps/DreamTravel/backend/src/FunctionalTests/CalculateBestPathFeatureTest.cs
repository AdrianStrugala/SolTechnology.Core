using System.Collections.Generic;
using Xbehave;
using Xunit;
using AutoFixture;
using DreamTravel.Api;
using DreamTravel.FunctionalTests.TestsConfiguration;
using DreamTravel.Trips.Domain.Cities;
using SolTechnology.Core.Api.Testing;
using System.Net.Http;
using DreamTravel.GeolocationData.GoogleApi;
using FluentAssertions;
using SolTechnology.Core.Api;
using SolTechnology.Core.Faker;

namespace DreamTravel.FunctionalTests
{
    [Collection(nameof(DreamTravelFunctionalTestsCollection))]
    public class CalculateBestPathFeatureTest
    {
        // private readonly ApiFixture _apiFixture;
        private readonly Fixture _fixture;
        private readonly HttpClient _apiClient;
        private readonly WireMockFixture _wireMockFixture;

        public CalculateBestPathFeatureTest(FunctionalTestsFixture functionalTestsFixture)
        {
            _apiClient = functionalTestsFixture.ApiFixture.ServerClient;
            _wireMockFixture = functionalTestsFixture.WireMockFixture;
            _fixture = new Fixture();
        }


        [Scenario(DisplayName = "Search for 3 cities and calculate best path")]
        public void Register_Login_CreateOrder_GetOrder(List<City> cities)
        {
            "Given is list of cities".x(() =>
            {
                cities = new List<City>
                {
                    new() { Name = "Wroclaw", Latitude =  51.107883, Longitude = 17.038538},
                    new() { Name = "Firenze", Latitude =  43.769562, Longitude = 11.255814},
                    new() { Name = "Vienna", Latitude = 48.210033, Longitude =  16.363449},
                    new() { Name = "Barcelona",  Latitude =  41.390205, Longitude = 2.154007}
                };
            });

            "Given is fake google API".x(() =>
            {
                foreach (var city in cities)
                {
                    _wireMockFixture.Fake<IGoogleApiClient>()
                        .WithRequest(x => x.GetLocationOfCity,
                            new Dictionary<string, string> { { "cityName", city.Name }, { "key", "googleKey" } })
                        .WithResponse(x => x.WithSuccess().WithBodyAsJson("xx"));
                }

                cities = new List<City>
                {
                    new() { Name = "Wroclaw", Latitude =  51.107883, Longitude = 17.038538},
                    new() { Name = "Firenze", Latitude =  43.769562, Longitude = 11.255814},
                    new() { Name = "Vienna", Latitude = 48.210033, Longitude =  16.363449},
                    new() { Name = "Barcelona",  Latitude =  41.390205, Longitude = 2.154007}
                };
            });


            "When User searches for location of each of them".x(async () =>
            {
                foreach (var city in cities)
                {
                    var apiResponse = await _apiClient
                        .CreateRequest("/api/FindLocationOfCity")
                        .WithHeader("Authorization", "DreamAuthentication U29sVWJlckFsbGVz")
                        .WithBody(new { Name = city.Name })
                        .PostAsync<ResponseEnvelope<City>>();

                    apiResponse.IsSuccess.Should().BeTrue();
                    apiResponse.Data.Should().BeEquivalentTo(city);
                }



            });

            "When User is Logged In".x(async () =>
            {
                // var loginResponse = await _apiFixture.InternalApiIntegrationTestsFixture.PostAsync<User>($"/api/users/login", user);

                // user = loginResponse.GetBody();
                // Assert.Equal(HttpStatusCode.OK, loginResponse.HttpStatusCode);
                // Assert.NotEqual(Guid.Empty, user.UserId);
            });

        }
    }
}

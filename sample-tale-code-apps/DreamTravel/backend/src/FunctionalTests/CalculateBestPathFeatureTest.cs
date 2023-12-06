using System.Collections.Generic;
using Xbehave;
using Xunit;
using DreamTravel.FunctionalTests.TestsConfiguration;
using DreamTravel.Trips.Domain.Cities;
using System.Net.Http;
using DreamTravel.FunctionalTests.FakeApis;
using DreamTravel.GeolocationData.GoogleApi;
using DreamTravel.Trips.Domain.Paths;
using FluentAssertions;
using SolTechnology.Core.Api;
using SolTechnology.Core.Faker;
using DreamTravel.Trips.Queries.CalculateBestPath;

namespace DreamTravel.FunctionalTests
{
    [Collection(nameof(DreamTravelFunctionalTestsCollection))]
    public class CalculateBestPathFeatureTest
    {
        private readonly HttpClient _apiClient;
        private readonly WireMockFixture _wireMockFixture;

        public CalculateBestPathFeatureTest(FunctionalTestsFixture functionalTestsFixture)
        {
            _apiClient = functionalTestsFixture.ApiFixture.ServerClient;
            _wireMockFixture = functionalTestsFixture.WireMockFixture;
        }


        [Scenario(DisplayName = "Search for 4 cities and calculate best path")]
        public void Register_Login_CreateOrder_GetOrder(List<City> cities)
        {
            List<Path> paths = null;

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

            "Given is fake google city API".x(() =>
            {
                foreach (var city in cities)
                {
                    _wireMockFixture.Fake<IGoogleApiClient>()
                        .WithRequest(x => x.GetLocationOfCity, city.Name)
                        .WithResponse(x => x.WithSuccess().WithBody(
                        $@"{{
                           ""results"" : 
                           [
                              {{
                                 ""geometry"" : 
                                 {{
                                    ""location"" : 
                                    {{
                                       ""lat"" : {city.Latitude},
                                       ""lng"" : {city.Longitude}
                                    }}
                                 }}
                              }}
                           ],
                           ""status"" : ""OK""
                        }}"));
                }
            });

            "Given is fake google distance API".x(() =>
            {
                _wireMockFixture.Fake<IGoogleApiClient>()
                    .WithRequest(x => x.GetDurationMatrixByFreeRoad, cities)
                    .WithResponse(x => x.WithSuccess().WithBody(GoogleFakeApi.FreeDistanceMatrix));

                _wireMockFixture.Fake<IGoogleApiClient>()
                    .WithRequest(x => x.GetDurationMatrixByTollRoad, cities)
                    .WithResponse(x => x.WithSuccess().WithBody(GoogleFakeApi.TollDistanceMatrix));
            });


            "When user searches for location of each of the cities".x(async () =>
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

            "And when user searches for the best path".x(async () =>
            {
                var apiResponse = await _apiClient
                    .CreateRequest("/api/CalculateBestPath")
                    .WithHeader("Authorization", "DreamAuthentication U29sVWJlckFsbGVz")
                    .WithBody(new { Cities = cities })
                    .PostAsync<ResponseEnvelope<CalculateBestPathResult>>();

                apiResponse.IsSuccess.Should().BeTrue();
                paths = apiResponse.Data.BestPaths;
            });


            "Then returned path is optimal".x(() =>
            {
                paths[0].StartingCity.Name.Should().Be("Wroclaw");
                paths[0].EndingCity.Name.Should().Be("Vienna");

                paths[1].StartingCity.Name.Should().Be("Vienna");
                paths[1].EndingCity.Name.Should().Be("Firenze");

                paths[2].StartingCity.Name.Should().Be("Firenze");
                paths[2].EndingCity.Name.Should().Be("Barcelona");
            });
        }
    }
}

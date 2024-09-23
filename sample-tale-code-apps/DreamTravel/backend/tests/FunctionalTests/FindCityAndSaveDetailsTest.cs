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
using SolTechnology.Core.CQRS;

namespace DreamTravel.FunctionalTests
{
    [Collection(nameof(DreamTravelFunctionalTestsCollection))]
    public class FindCityAndSaveDetailsTest
    {
        private readonly HttpClient _apiClient;
        private readonly WireMockFixture _wireMockFixture;

        public FindCityAndSaveDetailsTest(FunctionalTestsFixture functionalTestsFixture)
        {
            _apiClient = functionalTestsFixture.ApiFixture.ServerClient;
            _wireMockFixture = functionalTestsFixture.WireMockFixture;
        }


        [Scenario(DisplayName = "Search for a city and store its details using background processing")]
        public void Search_and_store_details(City city)
        {
            List<Path> paths = null;

            "Given is city".x(() =>
            {
                city = new() { Name = "Wroclaw", Latitude =  51.107883, Longitude = 17.038538
            };

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
                        .CreateRequest("/api/v2/FindCityByName")
                        .WithHeader("Authorization", "DreamAuthentication U29sVWJlckFsbGVz")
                        .WithBody(new { Name = city.Name })
                        .PostAsync<Result<City>>();

                    apiResponse.IsSuccess.Should().BeTrue();
                    apiResponse.Data.Should().BeEquivalentTo(city);
                }
            });

            "And when user searches for the best path".x(async () =>
            {
                var apiResponse = await _apiClient
                    .CreateRequest("/api/v2/CalculateBestPath")
                    .WithHeader("Authorization", "DreamAuthentication U29sVWJlckFsbGVz")
                    .WithBody(new { Cities = cities })
                    .PostAsync<Result<CalculateBestPathResult>>();

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

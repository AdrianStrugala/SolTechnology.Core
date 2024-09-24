using System.Collections.Generic;
using DreamTravel.Trips.Domain.Cities;
using System.Net.Http;
using System.Threading.Tasks;
using DreamTravel.GeolocationData.GoogleApi;
using DreamTravel.Trips.Domain.Paths;
using FluentAssertions;
using SolTechnology.Core.Faker;
using SolTechnology.Core.CQRS;
using NUnit.Framework;

namespace DreamTravel.FunctionalTests.Trips
{
    public class FindCityAndSaveDetailsTest
    {
        private HttpClient _apiClient;
        private WireMockFixture _wireMockFixture;

        [SetUp]
        public void Setup()
        {
            _apiClient = IntegrationTestsFixture.ApiFixture.ServerClient;
            _wireMockFixture = IntegrationTestsFixture.WireMockFixture;
        }

        [Test]
        public async Task Search_and_store_details()
        {
            List<Path> paths = null;

                City city = new()
                {
                    Name = "Wroclaw",
                    Latitude = 51.107883,
                    Longitude = 17.038538
                };

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

                var apiResponse = await _apiClient
                    .CreateRequest("/api/v2/FindCityByName")
                    .WithHeader("Authorization", "DreamAuthentication U29sVWJlckFsbGVz")
                    .WithBody(new { city.Name })
                    .PostAsync<Result<City>>();

                apiResponse.IsSuccess.Should().BeTrue();
                apiResponse.Data.Should().BeEquivalentTo(city);

            // "Then background job is triggered, city details fetched and stored".x(() =>
        }
    }
}

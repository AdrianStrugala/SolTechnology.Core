// using System.Collections.Generic;
// using Xbehave;
// using Xunit;
// using DreamTravel.Trips.Domain.Cities;
// using System.Net.Http;
// using DreamTravel.GeolocationData.GoogleApi;
// using DreamTravel.Trips.Domain.Paths;
// using FluentAssertions;
// using SolTechnology.Core.Faker;
// using SolTechnology.Core.CQRS;
//
// namespace DreamTravel.FunctionalTests.Trips
// {
//     public class FindCityAndSaveDetailsTest
//     {
//         private readonly HttpClient _apiClient;
//         private readonly WireMockFixture _wireMockFixture;
//
//         public FindCityAndSaveDetailsTest(IntegrationTestsFixture integrationTestsFixture)
//         {
//             _apiClient = integrationTestsFixture.ApiFixture.ServerClient;
//             _wireMockFixture = integrationTestsFixture.WireMockFixture;
//         }
//
//
//         [Scenario(DisplayName = "Search for a city and store its details using background processing")]
//         public void Search_and_store_details(City city)
//         {
//             List<Path> paths = null;
//
//             "Given is city".x(() =>
//             {
//                 city = new()
//                 {
//                     Name = "Wroclaw",
//                     Latitude = 51.107883,
//                     Longitude = 17.038538
//                 };
//             });
//
//             "Given is fake google city API".x(() =>
//                 {
//                     _wireMockFixture.Fake<IGoogleApiClient>()
//                         .WithRequest(x => x.GetLocationOfCity, city.Name)
//                         .WithResponse(x => x.WithSuccess().WithBody(
//                             $@"{{
//                            ""results"" : 
//                            [
//                               {{
//                                  ""geometry"" : 
//                                  {{
//                                     ""location"" : 
//                                     {{
//                                        ""lat"" : {city.Latitude},
//                                        ""lng"" : {city.Longitude}
//                                     }}
//                                  }}
//                               }}
//                            ],
//                            ""status"" : ""OK""
//                         }}"));
//                 });
//
//             "When user searches for location of the city".x(async () =>
//             {
//                 var apiResponse = await _apiClient
//                     .CreateRequest("/api/v2/FindCityByName")
//                     .WithHeader("Authorization", "DreamAuthentication U29sVWJlckFsbGVz")
//                     .WithBody(new { city.Name })
//                     .PostAsync<Result<City>>();
//
//                 apiResponse.IsSuccess.Should().BeTrue();
//                 apiResponse.Data.Should().BeEquivalentTo(city);
//             });
//
//             "Then background job is triggered, city details fetched and stored".x(() =>
//             {
//                 paths[0].StartingCity.Name.Should().Be("Wroclaw");
//                 paths[0].EndingCity.Name.Should().Be("Vienna");
//
//                 paths[1].StartingCity.Name.Should().Be("Vienna");
//                 paths[1].EndingCity.Name.Should().Be("Firenze");
//
//                 paths[2].StartingCity.Name.Should().Be("Firenze");
//                 paths[2].EndingCity.Name.Should().Be("Barcelona");
//             });
//         }
//     }
// }

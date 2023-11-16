using System;
using System.Collections.Generic;
using System.Net;
using Xbehave;
using Xunit;
using AutoFixture;
using DreamTravel.Api;
using DreamTravel.FunctionalTests.Models;
using DreamTravel.FunctionalTests.TestsConfiguration;
using DreamTravel.TestFixture.Api.TestsConfiguration;
using DreamTravel.Trips.Domain.Cities;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.TestHost;
using SolTechnology.Core.Api.Testing;
using System.Net.Http;
using SolTechnology.Core.Api;

namespace DreamTravel.FunctionalTests
{
    [Collection(nameof(DreamTravelFunctionalTestsCollection))]
    public class CalculateBestPathFeatureTest
    {
        // private readonly ApiFixture _apiFixture;
        private readonly Fixture _fixture;
        private readonly ApiFixture<Program> _apiFixture;
        private readonly HttpClient _apiClient;

        public CalculateBestPathFeatureTest(FunctionalTestsFixture functionalTestsFixture)
        {
            // _apiFixture = apiFixture;
            _apiFixture = functionalTestsFixture.ApiFixture;
            _apiClient = functionalTestsFixture.ApiFixture.ServerClient;
            _fixture = new Fixture();
        }


        [Scenario(DisplayName = "Search for 3 cities and calculate best path")]
        public void Register_Login_CreateOrder_GetOrder(List<City> cities)
        {
            "Given is list of cities".x(() =>
            {
                cities = new List<City>
                {
                    new() { Name = "Wroclaw" },
                    new() { Name = "Vienna" },
                    new() { Name = "Barcelona" }
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

                    Assert.True(apiResponse.IsSuccess);

                    //Assert data (add wiremock)
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

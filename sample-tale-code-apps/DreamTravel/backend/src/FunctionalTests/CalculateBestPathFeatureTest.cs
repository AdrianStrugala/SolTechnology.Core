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

namespace DreamTravel.FunctionalTests
{
    // [Collection(nameof(ApiFunctionalTests))]
    public class CalculateBestPathFeatureTest
    {
        // private readonly ApiFixture _apiFixture;
        private readonly Fixture _fixture;
        private readonly ApiFixture<Program> _apiFixture;

        public CalculateBestPathFeatureTest(FunctionalTestsFixture functionalTestsFixture)
        {
            // _apiFixture = apiFixture;
            _apiFixture = functionalTestsFixture.ApiFixture;
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
                    var apiResponse = await _apiFixture.ServerClient.PostAsync<City>("/api/FindLocationOfCity", new { Name = city.Name });
                    //
                    // var apiResponse = await _api
                    //     .CreateRequest($"/api/FindLocationOfCity")
                    //     // .AddHeader("X-Auth", "SolTechnologyAuthentication U2VjdXJlS2V5")
                    //     .PostAsync();
                    // _api.
                    // var apiResponse = await _apiFixture.InternalApiIntegrationTestsFixture.PostAsync<City>($"/api/FindLocationOfCity", new { Name = city.Name });
                    // Assert.Equal(HttpStatusCode.OK, apiResponse.);
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

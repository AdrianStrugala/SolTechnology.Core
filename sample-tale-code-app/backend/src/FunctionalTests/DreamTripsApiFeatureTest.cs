using System;
using System.Net;
using Xbehave;
using Xunit;
using AutoFixture;
using DreamTravel.FunctionalTests.Models;
using DreamTravel.FunctionalTests.TestsConfiguration;
using DreamTravel.TestFixture.Api.TestsConfiguration;

namespace DreamTravel.FunctionalTests
{
    [Collection(nameof(ApiFunctionalTests))]
    public class DreamTripsApiFeatureTest
    {
        private readonly ApiFixture _apiFixture;
        private readonly Fixture _fixture;

        public DreamTripsApiFeatureTest(ApiFixture apiFixture)
        {
            _apiFixture = apiFixture;
            _fixture = new Fixture();
        }


        //TODO: it will be calculating path, saving it and returning

        [Scenario(DisplayName = "Register and Login User, Create Subscription and Get Subscription")]
        public void Register_Login_CreateOrder_GetOrder(User user)
        {
            user = new User
            {
                Email = "TestEmail",
                Password = "TestPassword",
                Name = "TestUser"
            };

            "When User is Registered".x(async () =>
            {
                var registerResponse = await _apiFixture.InternalApiIntegrationTestsFixture.PostAsync<EmptyResponse>($"/api/users/register", user);
                Assert.Equal(HttpStatusCode.OK, registerResponse.HttpStatusCode);
            });

            "When User is Logged In".x(async () =>
            {
                var loginResponse = await _apiFixture.InternalApiIntegrationTestsFixture.PostAsync<User>($"/api/users/login", user);

                user = loginResponse.GetBody();
                Assert.Equal(HttpStatusCode.OK, loginResponse.HttpStatusCode);
                Assert.NotEqual(Guid.Empty, user.UserId);
            });

        }
    }
}

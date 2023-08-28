using System;
using System.Collections.Generic;
using System.Net;
using Xbehave;
using Xunit;
using AutoFixture;
using DreamTravel.ApiTests.TestsConfiguration;
using DreamTravel.DatabaseData.Query.GetSubscriptionsWithDays;
using DreamTravel.DreamFlights.SubscribeForFlightEmail;
using DreamTravel.FunctionalTests.Models;
using DreamTravel.FunctionalTests.TestsConfiguration;

namespace DreamTravel.FunctionalTests
{
    [Collection(nameof(ApiFunctionalTests))]
    public class DreamFlightsApiFeatureTest
    {
        private readonly ApiFixture _apiFixture;
        private readonly Fixture _fixture;

        public DreamFlightsApiFeatureTest(ApiFixture apiFixture)
        {
            _apiFixture = apiFixture;
            _fixture = new Fixture();
        }

        [Scenario(DisplayName = "Register and Login User, Create Subscription and Get Subscription")]
        public void Register_Login_CreateOrder_GetOrder(User user, SubscribeForFlightEmailsCommand subscriptionCommand)
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

            "When User creates FlightEmailSubscription".x(async () =>
            {
                subscriptionCommand = _fixture.Create<SubscribeForFlightEmailsCommand>();
                subscriptionCommand.FlightEmailSubscription.UserId = user.UserId;

                var postSubscriptionResponse = await _apiFixture.InternalApiIntegrationTestsFixture.PostAsync<EmptyResponse>($"/api/FlightEmailSubscription", subscriptionCommand);

                Assert.Equal(HttpStatusCode.OK, postSubscriptionResponse.HttpStatusCode);
            });

            "Then User get his FlightEmailSubscription".x(async () =>
            {
                var getSubscriptionResponse = await _apiFixture.InternalApiIntegrationTestsFixture.GetAsync<List<SubscriptionWithDays>>($"api/FlightEmailSubscription/{user.UserId}");

                Assert.Equal(HttpStatusCode.OK, getSubscriptionResponse.HttpStatusCode);
                var subscriptionResult = Assert.Single(getSubscriptionResponse.GetBody());
                Assert.Equal(subscriptionCommand.FlightEmailSubscription.UserId, subscriptionResult.UserId);
                Assert.Equal(subscriptionCommand.FlightEmailSubscription.ArrivalDate.Date, subscriptionResult.ArrivalDate.Date);
                Assert.Equal(subscriptionCommand.FlightEmailSubscription.DepartureDate.Date, subscriptionResult.DepartureDate.Date);
                Assert.Equal(subscriptionCommand.FlightEmailSubscription.From, subscriptionResult.From);
                Assert.Equal(subscriptionCommand.FlightEmailSubscription.To, subscriptionResult.To);
                Assert.Equal(subscriptionCommand.FlightEmailSubscription.MaxDaysOfStay, subscriptionResult.MaxDaysOfStay);
                Assert.Equal(subscriptionCommand.FlightEmailSubscription.MinDaysOfStay, subscriptionResult.MinDaysOfStay);
                Assert.Equal(subscriptionCommand.FlightEmailSubscription.OneWay, subscriptionResult.OneWay);
                Assert.Equal(subscriptionCommand.SubscriptionDays.Friday, subscriptionResult.Friday);
                Assert.Equal(subscriptionCommand.SubscriptionDays.Monday, subscriptionResult.Monday);
                Assert.Equal(subscriptionCommand.SubscriptionDays.Saturday, subscriptionResult.Saturday);
                Assert.Equal(subscriptionCommand.SubscriptionDays.Sunday, subscriptionResult.Sunday);
                Assert.Equal(subscriptionCommand.SubscriptionDays.Thursday, subscriptionResult.Thursday);
                Assert.Equal(subscriptionCommand.SubscriptionDays.Tuesday, subscriptionResult.Tuesday);
                Assert.Equal(subscriptionCommand.SubscriptionDays.Wednesday, subscriptionResult.Wednesday);
            });
        }
    }
}

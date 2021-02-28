using System;
using System.Collections.Generic;
using System.Linq;
using DreamTravel.DatabaseData.Query.GetSubscriptionDetailsByDay;
using DreamTravel.DatabaseData.Repository.FlightEmailSubscriptions;
using DreamTravel.DatabaseData.Repository.Users;
using DreamTravel.DatabaseDataTests.TestsConfiguration;
using DreamTravel.Domain.FlightEmailSubscriptions;
using DreamTravel.Domain.Users;
using Xunit;

namespace DreamTravel.DatabaseDataTests.FlightEmailSubscriptions
{
    [Collection(nameof(TestsCollections.SqlTestsCollection))]
    public class FlightEmailSubscriptionRepositoryTests
    {
        private readonly FlightEmailSubscriptionRepository _sut;
        private readonly FlightEmailSubscriptionFactory _subscriptionFactory;
        private readonly UserRepository _users;

        public FlightEmailSubscriptionRepositoryTests(SqlFixture sqlFixture)
        {
            var dbConnectionFactory = sqlFixture.DbConnectionFactory;
            _sut = new FlightEmailSubscriptionRepository(dbConnectionFactory);
            _subscriptionFactory = new FlightEmailSubscriptionFactory(dbConnectionFactory);

            _users = new UserRepository(dbConnectionFactory);
        }

        [Fact]
        public void Insert_ValidOrderIncoming_ItIsSavedInDB()
        {
            //Arrange
            User user = new User(
                name: "test",
                password: "password",
                email: "xd@Insert.pl"
            );

            _users.Insert(user);

            FlightEmailSubscription flightEmailSubscription = new FlightEmailSubscription();
            flightEmailSubscription.UserId = user.UserId;
            flightEmailSubscription.ArrivalDate = DateTime.UtcNow.AddDays(3);
            flightEmailSubscription.DepartureDate = DateTime.UtcNow;
            flightEmailSubscription.From = "SOME UNIQUE STRING";
            flightEmailSubscription.To = "London";
            flightEmailSubscription.MaxDaysOfStay = 4;
            flightEmailSubscription.MinDaysOfStay = 1;
            flightEmailSubscription.OneWay = false;

            //Act
            _sut.Insert(flightEmailSubscription);

            //Assert
            List<FlightEmailData> flightEmailRecords = _subscriptionFactory.GetSubscriptionData();

            Assert.NotNull(flightEmailRecords);
            Assert.NotEmpty(flightEmailRecords);

            var orderUnderTest = flightEmailRecords.Single(o => o.From == "SOME UNIQUE STRING");

            Assert.Equal(flightEmailSubscription.ArrivalDate.Date, orderUnderTest.ArrivalDate.Date);
            Assert.Equal(flightEmailSubscription.DepartureDate.Date, orderUnderTest.DepartureDate.Date);
            Assert.Equal(flightEmailSubscription.From, orderUnderTest.From);
            Assert.Equal(flightEmailSubscription.To, orderUnderTest.To);
            Assert.Equal(flightEmailSubscription.MaxDaysOfStay, orderUnderTest.MaxDaysOfStay);
            Assert.Equal(flightEmailSubscription.MinDaysOfStay, orderUnderTest.MinDaysOfStay);
            Assert.Equal(flightEmailSubscription.OneWay, orderUnderTest.OneWay);
            Assert.Equal(flightEmailSubscription.UserId, orderUnderTest.UserId);
        }


        [Fact]
        public void GetByUserId_ValidOrder_ReturnedRecordsAreOnlyForThisUser()
        {
            //Arrange
            User user = new User
            (
                name: "GetByUserId",
                password: "password",
                email: "validUser@GetByUserId"
            );

            User anotherUser = new User
            (
                name: "GetByUserIdNotGood",
                password: "password",
                email: "anotherUser@GetByUserId"
            );

            _users.Insert(user);
            _users.Insert(anotherUser);


            _subscriptionFactory.InsertFlightEmailSubscriptionForUser(user.UserId);
            _subscriptionFactory.InsertFlightEmailSubscriptionForUser(user.UserId);
            _subscriptionFactory.InsertFlightEmailSubscriptionForUser(user.UserId);

            _subscriptionFactory.InsertFlightEmailSubscriptionForUser(anotherUser.UserId);


            //Act
            var result = _sut.GetByUserId(user.UserId);


            //Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal(3, result.Count);

        }

        [Fact]
        public void Delete_ValidOrder_ItemWasDeletedFromDB()
        {
            //Arrange
            User user = new User
            (
                name: "test",
                password: "password",
                email: "xd@delete.pl"
            );

            _users.Insert(user);

            var orderUnderTest = _subscriptionFactory.InsertFlightEmailSubscriptionForUser(user.UserId);


            //Assert
            List<FlightEmailData> flightEmailData = _subscriptionFactory.GetSubscriptionData();

            Assert.NotNull(flightEmailData);
            Assert.NotEmpty(flightEmailData);


            //Act
            _sut.Delete(orderUnderTest.Id);


            //Assert 2
            List<FlightEmailData> afterDelete = _subscriptionFactory.GetSubscriptionData();

            Assert.NotNull(afterDelete);
            Assert.Empty(afterDelete);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using DreamTravel.DatabaseData.FlightEmailSubscriptions;
using DreamTravel.Domain.FlightEmailSubscriptions;
using DreamTravel.Domain.Users;
using DreamTravel.Infrastructure.Database;
using Xunit;

namespace DreamTravel.DatabaseDataTests.FlightEmailSubscriptions
{
    public class FlightEmailSubscriptionRepositoryTests : IClassFixture<SqlFixture>
    {
        private readonly FlightEmailSubscriptionRepository _sut;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly FlightEmailSubscriptionFactory _subscriptionFactory;

        public FlightEmailSubscriptionRepositoryTests(SqlFixture sqlFixture)
        {
            _dbConnectionFactory = sqlFixture.DbConnectionFactory;
            _sut = new FlightEmailSubscriptionRepository(_dbConnectionFactory);
            _subscriptionFactory = new FlightEmailSubscriptionFactory(_dbConnectionFactory);
        }

        [Fact]
        public void Insert_ValidOrderIncoming_ItIsSavedInDB()
        {
            //Arrange
            User user = new User();
            user.Name = "test";
            user.Email = "xd@Insert.pl";

            string insertSql = @"
INSERT INTO [User] ([Name], Email)
OUTPUT INSERTED.ID
VALUES (@Name, @Email)";

            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                user.Id = connection.QuerySingle<int>(insertSql, new { Name = user.Name, Email = user.Email });
            }

            FlightEmailSubscription flightEmailSubscription = new FlightEmailSubscription();
            flightEmailSubscription.UserId = user.Id;
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
            {
                Name = "GetByUserId",
                Email = "validUser@GetByUserId"
            };

            User anotherUser = new User
            {
                Name = "GetByUserIdNotGood",
                Email = "anotherUser@@GetByUserId"
            };

            string insertSql = @"
INSERT INTO [User] ([Name], Email)
OUTPUT INSERTED.ID
VALUES (@Name, @Email)";

            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                user.Id = connection.QuerySingle<int>(insertSql, new { Name = user.Name, Email = user.Email });
                anotherUser.Id = connection.QuerySingle<int>(insertSql, new { Name = anotherUser.Name, Email = anotherUser.Email });
            }


            _subscriptionFactory.InsertFlightEmailSubscriptionForUser(user.Id);
            _subscriptionFactory.InsertFlightEmailSubscriptionForUser(user.Id);
            _subscriptionFactory.InsertFlightEmailSubscriptionForUser(user.Id);

            _subscriptionFactory.InsertFlightEmailSubscriptionForUser(anotherUser.Id);


            //Act
            var result = _sut.GetByUserId(user.Id);


            //Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal(3, result.Count);

        }

        [Fact]
        public void Delete_ValidOrder_ItemWasDeletedFromDB()
        {
            //Arrange
            User user = new User();
            user.Name = "test";
            user.Email = "xd@delete.pl";

            string insertSql = @"
INSERT INTO [User] ([Name], Email)
OUTPUT INSERTED.ID
VALUES (@Name, @Email)";

            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                user.Id = connection.QuerySingle<int>(insertSql, new { Name = user.Name, Email = user.Email });
            }


            var orderUnderTest = _subscriptionFactory.InsertFlightEmailSubscriptionForUser(user.Id);

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

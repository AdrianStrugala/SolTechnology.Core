using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using DreamTravel.DatabaseData.FlightEmailOrders;
using DreamTravel.Domain.FlightEmailOrders;
using DreamTravel.Domain.Users;
using DreamTravel.Infrastructure.Database;
using Xunit;

namespace DreamTravel.DatabaseDataTests.FlightEmailOrders
{
    public class FlightEmailOrderRepositoryTests : IClassFixture<SqlFixture>
    {
        private readonly FlightEmailOrderRepository _sut;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly FlightEmailOrderFactory _orderFactory;

        public FlightEmailOrderRepositoryTests(SqlFixture sqlFixture)
        {
            _dbConnectionFactory = sqlFixture.DbConnectionFactory;
            _sut = new FlightEmailOrderRepository(_dbConnectionFactory);
            _orderFactory = new FlightEmailOrderFactory(_dbConnectionFactory);
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

            FlightEmailOrder flightEmailOrder = new FlightEmailOrder();
            flightEmailOrder.UserId = user.Id;
            flightEmailOrder.ArrivalDate = DateTime.UtcNow.AddDays(3);
            flightEmailOrder.DepartureDate = DateTime.UtcNow;
            flightEmailOrder.From = "SOME UNIQUE STRING";
            flightEmailOrder.To = "London";
            flightEmailOrder.MaxDaysOfStay = 4;
            flightEmailOrder.MinDaysOfStay = 1;
            flightEmailOrder.OneWay = false;

            //Act
            _sut.Insert(flightEmailOrder);

            //Assert
            List<FlightEmailData> flightEmailOrders = _sut.GetAll();

            Assert.NotNull(flightEmailOrders);
            Assert.NotEmpty(flightEmailOrders);

            var orderUnderTest = flightEmailOrders.Single(o => o.From == "SOME UNIQUE STRING");

            Assert.Equal(flightEmailOrder.ArrivalDate.Date, orderUnderTest.ArrivalDate.Date);
            Assert.Equal(flightEmailOrder.DepartureDate.Date, orderUnderTest.DepartureDate.Date);
            Assert.Equal(flightEmailOrder.From, orderUnderTest.From);
            Assert.Equal(flightEmailOrder.To, orderUnderTest.To);
            Assert.Equal(flightEmailOrder.MaxDaysOfStay, orderUnderTest.MaxDaysOfStay);
            Assert.Equal(flightEmailOrder.MinDaysOfStay, orderUnderTest.MinDaysOfStay);
            Assert.Equal(flightEmailOrder.OneWay, orderUnderTest.OneWay);
            Assert.Equal(flightEmailOrder.UserId, orderUnderTest.UserId);
        }


        [Fact]
        public void GetByUserId_ValidQuery_ReturnedRecordsAreOnlyForThisUser()
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


            _orderFactory.InsertFlightEmailOrderForUser(user.Id);
            _orderFactory.InsertFlightEmailOrderForUser(user.Id);
            _orderFactory.InsertFlightEmailOrderForUser(user.Id);

            _orderFactory.InsertFlightEmailOrderForUser(anotherUser.Id);


            //Act
            var result = _sut.GetByUserId(user.Id);


            //Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal(3, result.Count);

        }
    }
}

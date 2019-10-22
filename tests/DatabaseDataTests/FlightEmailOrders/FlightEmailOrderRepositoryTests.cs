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
        private IDbConnectionFactory _dbConnectionFactory;

        public FlightEmailOrderRepositoryTests(SqlFixture sqlFixture)
        {
            _dbConnectionFactory = sqlFixture.DbConnectionFactory;
            _sut = new FlightEmailOrderRepository(_dbConnectionFactory);
        }

        [Fact]
        public void Insert_ValidOrderIncoming_ItIsSavedInDB()
        {
            //Arrange
            User user = new User();
            user.Name = "test";
            user.Email = "xd";

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
            flightEmailOrder.From = "Poland";
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

            Assert.Equal(flightEmailOrder.ArrivalDate.Date, flightEmailOrders.First().ArrivalDate.Date);
            Assert.Equal(flightEmailOrder.DepartureDate.Date, flightEmailOrders.First().DepartureDate.Date);
            Assert.Equal(flightEmailOrder.From, flightEmailOrders.First().From);
            Assert.Equal(flightEmailOrder.To, flightEmailOrders.First().To);
            Assert.Equal(flightEmailOrder.MaxDaysOfStay, flightEmailOrders.First().MaxDaysOfStay);
            Assert.Equal(flightEmailOrder.MinDaysOfStay, flightEmailOrders.First().MinDaysOfStay);
            Assert.Equal(flightEmailOrder.OneWay, flightEmailOrders.First().OneWay);
            Assert.Equal(flightEmailOrder.UserId, flightEmailOrders.First().UserId);
        }
    }
}

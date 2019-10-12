using System;
using System.Collections.Generic;
using System.Linq;
using DreamTravel.DatabaseData.FlightEmailOrders;
using DreamTravel.Domain.FlightEmailOrders;
using Xunit;

namespace DreamTravel.DatabaseDataTests.FlightEmailOrders
{
    public class FlightEmailOrderRepositoryTests : IClassFixture<SqlFixture>
    {
        private readonly FlightEmailOrderRepository _sut;

        public FlightEmailOrderRepositoryTests(SqlFixture sqlFixture)
        {
            _sut = new FlightEmailOrderRepository(sqlFixture.DbConnectionFactory);
        }

        [Fact]
        public void Insert_ValidOrderIncoming_ItIsSavedInDB()
        {
            //Arrange
            FlightEmailOrder flightEmailOrder = new FlightEmailOrder();
            flightEmailOrder.UserId = 1;
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

            Assert.Equal(flightEmailOrder.ArrivalDate, flightEmailOrders.First().ArrivalDate);
            Assert.Equal(flightEmailOrder.DepartureDate, flightEmailOrders.First().DepartureDate);
            Assert.Equal(flightEmailOrder.From, flightEmailOrders.First().From);
            Assert.Equal(flightEmailOrder.To, flightEmailOrders.First().To);
            Assert.Equal(flightEmailOrder.MaxDaysOfStay, flightEmailOrders.First().MaxDaysOfStay);
            Assert.Equal(flightEmailOrder.MinDaysOfStay, flightEmailOrders.First().MinDaysOfStay);
            Assert.Equal(flightEmailOrder.OneWay, flightEmailOrders.First().OneWay);
            Assert.Equal(flightEmailOrder.UserId, flightEmailOrders.First().UserId);
        }
    }
}

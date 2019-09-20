using System;
using System.Collections.Generic;
using DreamTravel.Bot.DiscoverIndividualChances.Models;
using DreamTravel.DatabaseData.FlightEmailOrders;
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
            List<FlightEmailOrder> flightEmailOrders = _sut.GetAll();

            Assert.Contains(flightEmailOrder, flightEmailOrders);
        }
    }
}

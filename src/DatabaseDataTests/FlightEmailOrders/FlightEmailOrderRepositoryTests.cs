using System;
using System.Collections.Generic;
using DreamTravel.Bot.DiscoverIndividualChances.Models;
using DreamTravel.DatabaseData.FlightEmailOrders;
using DreamTravel.Infrastructure.Database;
using Xunit;

namespace DreamTravel.DatabaseDataTests.FlightEmailOrders
{
    public class FlightEmailOrderRepositoryTests
    {
        private readonly FlightEmailOrderRepository _sut;

        public FlightEmailOrderRepositoryTests()
        {
            string connectionString =
                "Server=tcp:dreamtravel.database.windows.net,1433;Initial Catalog=dreamtravel;Persist Security Info=False;User ID=adrian;Password=P4ssw0rd@;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;";
            var dbConnectionFactory = new DbConnectionFactory(connectionString);

            _sut = new FlightEmailOrderRepository(dbConnectionFactory);
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

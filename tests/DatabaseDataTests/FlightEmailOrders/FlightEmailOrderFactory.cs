using System;
using Dapper;
using DreamTravel.Domain.FlightEmailOrders;
using DreamTravel.Infrastructure.Database;

namespace DreamTravel.DatabaseDataTests.FlightEmailOrders
{
    public class FlightEmailOrderFactory
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public FlightEmailOrderFactory(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public FlightEmailOrder InsertFlightEmailOrderForUser(int userId)
        {
            FlightEmailOrder flightEmailOrder = new FlightEmailOrder();
            flightEmailOrder.UserId = userId;
            flightEmailOrder.ArrivalDate = DateTime.UtcNow.AddDays(3);
            flightEmailOrder.DepartureDate = DateTime.UtcNow;
            flightEmailOrder.From = "Poland";
            flightEmailOrder.To = "London";
            flightEmailOrder.MaxDaysOfStay = 4;
            flightEmailOrder.MinDaysOfStay = 1;
            flightEmailOrder.OneWay = false;

            string sql = @"
INSERT INTO FlightEmailOrder
([UserId], [From], [To], [DepartureDate], [ArrivalDate], [MinDaysOfStay], [MaxDaysOfStay], [OneWay])
VALUES
(@UserId, @From, @To, @DepartureDate, @ArrivalDate, @MinDaysOfStay, @MaxDaysOfStay, @OneWay)
";

            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                connection.Execute(sql, new
                {
                    UserId = flightEmailOrder.UserId,
                    From = flightEmailOrder.From,
                    To = flightEmailOrder.To,
                    DepartureDate = flightEmailOrder.DepartureDate,
                    ArrivalDate = flightEmailOrder.ArrivalDate,
                    MinDaysOfStay = flightEmailOrder.MinDaysOfStay,
                    MaxDaysOfStay = flightEmailOrder.MaxDaysOfStay,
                    OneWay = flightEmailOrder.OneWay
                });
            }

            return flightEmailOrder;
        }
    }
}

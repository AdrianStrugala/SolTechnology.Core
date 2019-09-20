using System.Collections.Generic;
using Dapper;
using DreamTravel.Bot.DiscoverIndividualChances.Models;
using DreamTravel.Infrastructure.Database;

namespace DreamTravel.DatabaseData.FlightEmailOrders
{
    public class FlightEmailOrderRepository : IFlightEmailOrderRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public FlightEmailOrderRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public void Insert(FlightEmailOrder flightEmailOrder)
        {
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
        }

        public List<FlightEmailOrder> GetAll()
        {
            throw new System.NotImplementedException();
        }
    }
}
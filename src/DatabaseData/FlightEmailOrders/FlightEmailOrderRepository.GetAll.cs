using System.Collections.Generic;
using System.Linq;
using Dapper;
using DreamTravel.Bot.DiscoverIndividualChances.Models;

namespace DreamTravel.DatabaseData.FlightEmailOrders
{
    public partial class FlightEmailOrderRepository : IFlightEmailOrderRepository
    {

        public List<FlightEmailOrder> GetAll()
        {
            var result = new List<FlightEmailOrder>();

            string sql = @"
SELECT 
FlightEmailOrder.[UserId],
FlightEmailOrder.[From],
FlightEmailOrder.[To],
FlightEmailOrder.[DepartureDate],
FlightEmailOrder.[ArrivalDate],
FlightEmailOrder.[MinDaysOfStay], 
FlightEmailOrder.[MaxDaysOfStay], 
FlightEmailOrder.[OneWay],
[User].Name,
[User].Email,
[User].Currency,

FROM FlightEmailOrder
JOIN [User] on [User].Id = FlightEmailOrder.[UserId]
";

            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                result = connection.Query<FlightEmailOrder>(sql).ToList();
            }

            return result;
        }
    }
}
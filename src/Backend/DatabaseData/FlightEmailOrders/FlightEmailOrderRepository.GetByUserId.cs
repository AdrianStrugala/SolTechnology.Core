using System.Collections.Generic;
using System.Linq;
using Dapper;
using DreamTravel.Domain.FlightEmailOrders;

namespace DreamTravel.DatabaseData.FlightEmailOrders
{
    public partial class FlightEmailOrderRepository : IFlightEmailOrderRepository
    {
        public List<FlightEmailOrder> GetByUserId(int userId)
        {
            List<FlightEmailOrder> result;

            string sql = @"
SELECT 
FlightEmailOrder.[Id],
FlightEmailOrder.[UserId],
FlightEmailOrder.[From],
FlightEmailOrder.[To],
FlightEmailOrder.[DepartureDate],
FlightEmailOrder.[ArrivalDate],
FlightEmailOrder.[MinDaysOfStay], 
FlightEmailOrder.[MaxDaysOfStay], 
FlightEmailOrder.[OneWay]

FROM FlightEmailOrder
WHERE
    [UserId] = @userId
";

            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                result = connection.Query<FlightEmailOrder>(sql, new { userId = userId }).ToList();
            }

            return result;
        }
    }
}
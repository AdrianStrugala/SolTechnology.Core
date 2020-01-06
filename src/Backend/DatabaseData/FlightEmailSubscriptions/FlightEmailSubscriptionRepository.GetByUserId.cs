using System.Collections.Generic;
using System.Linq;
using Dapper;
using DreamTravel.Domain.FlightEmailOrders;

namespace DreamTravel.DatabaseData.FlightEmailSubscriptions
{
    public partial class FlightEmailSubscriptionRepository : IFlightEmailSubscriptionRepository
    {
        public List<FlightEmailSubscription> GetByUserId(int userId)
        {
            List<FlightEmailSubscription> result;

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
                result = connection.Query<FlightEmailSubscription>(sql, new { userId = userId }).ToList();
            }

            return result;
        }
    }
}
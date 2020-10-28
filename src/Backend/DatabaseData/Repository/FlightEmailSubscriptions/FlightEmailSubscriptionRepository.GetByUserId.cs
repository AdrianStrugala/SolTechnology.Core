using System.Collections.Generic;
using System.Linq;
using Dapper;
using DreamTravel.Domain.FlightEmailSubscriptions;

namespace DreamTravel.DatabaseData.Repository.FlightEmailSubscriptions
{
    public partial class FlightEmailSubscriptionRepository : IFlightEmailSubscriptionRepository
    {
        public List<FlightEmailSubscription> GetByUserId(int userId)
        {
            List<FlightEmailSubscription> result;

            string sql = @"
SELECT 
FlightEmailSubscription.[Id],
FlightEmailSubscription.[UserId],
FlightEmailSubscription.[From],
FlightEmailSubscription.[To],
FlightEmailSubscription.[DepartureDate],
FlightEmailSubscription.[ArrivalDate],
FlightEmailSubscription.[MinDaysOfStay], 
FlightEmailSubscription.[MaxDaysOfStay], 
FlightEmailSubscription.[OneWay]

FROM FlightEmailSubscription
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
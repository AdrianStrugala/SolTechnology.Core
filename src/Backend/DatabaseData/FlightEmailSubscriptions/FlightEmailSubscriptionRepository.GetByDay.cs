using System.Collections.Generic;
using System.Linq;
using Dapper;
using DreamTravel.Domain.FlightEmailSubscriptions;

namespace DreamTravel.DatabaseData.FlightEmailSubscriptions
{
    public partial class FlightEmailSubscriptionRepository : IFlightEmailSubscriptionRepository
    {
        public List<FlightEmailData> GetByDay(string day)
        {
            var result = new List<FlightEmailData>();

            string sql = @"
SELECT 
FlightEmailSubscription.[UserId],
FlightEmailSubscription.[From],
FlightEmailSubscription.[To],
FlightEmailSubscription.[DepartureDate],
FlightEmailSubscription.[ArrivalDate],
FlightEmailSubscription.[MinDaysOfStay], 
FlightEmailSubscription.[MaxDaysOfStay], 
FlightEmailSubscription.[OneWay],
[User].[Name] AS UserName,
[User].Email,
[User].Currency

FROM FlightEmailSubscription
JOIN [User] on [User].Id = FlightEmailSubscription.[UserId]
JOIN [SubscriptionDays] on [SubscriptionDays].[FlightEmailSubscriptionId] = FlightEmailSubscription.[Id]

";

            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                sql += $@"
WHERE 
    [SubscriptionDays].[{day}] = 1
";

                result = connection.Query<FlightEmailData>(sql).ToList();
            }

            return result;
        }
    }
}
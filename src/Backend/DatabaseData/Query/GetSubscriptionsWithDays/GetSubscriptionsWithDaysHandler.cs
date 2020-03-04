using System.Linq;
using Dapper;
using DreamTravel.DatabaseData.Query.GetSubscriptionsWithDays;
using DreamTravel.Infrastructure.Database;

namespace DreamTravel.DatabaseData.FlightEmailSubscriptions
{
    public class GetSubscriptionsWithDaysHandler : IGetSubscriptionsWithDays
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public GetSubscriptionsWithDaysHandler(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public GetSubscriptionsWithDaysResult Execute(GetSubscriptionsWithDaysQuery query)
        {
            GetSubscriptionsWithDaysResult result = new GetSubscriptionsWithDaysResult();

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
,[SubscriptionDays].[Monday]
,[SubscriptionDays].[Tuesday]
,[SubscriptionDays].[Wednesday]
,[SubscriptionDays].[Thursday]
,[SubscriptionDays].[Friday]
,[SubscriptionDays].[Saturday]
,[SubscriptionDays].[Sunday]

FROM FlightEmailSubscription
JOIN [SubscriptionDays] on [SubscriptionDays].FlightEmailSubscriptionId = FlightEmailSubscription.[Id]
WHERE
    [UserId] = @userId
";

            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                result.SubscriptionsWithDays = connection.Query<SubscriptionWithDays>(sql, new { userId = query.UserId }).ToList();
            }

            return result;
        }
    }
}
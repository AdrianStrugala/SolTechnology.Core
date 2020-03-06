using System.Collections.Generic;
using System.Linq;
using Dapper;
using DreamTravel.Domain.FlightEmailSubscriptions;

namespace DreamTravel.DatabaseData.SubscriptionDays
{
    public partial class SubscriptionDaysRepository : ISubscriptionDaysRepository
    {

        private string GetByUserSql = @"
SELECT 
[FlightEmailSubscriptionId]
,[SubscriptionDays].[Monday]
,[SubscriptionDays].[Tuesday]
,[SubscriptionDays].[Wednesday]
,[SubscriptionDays].[Thursday]
,[SubscriptionDays].[Friday]
,[SubscriptionDays].[Saturday]
,[SubscriptionDays].[Sunday]

FROM FlightEmailSubscription
JOIN [SubscriptionDays] on [SubscriptionDays].FlightEmailSubscriptionId = FlightEmailSubscription.[Id]
  WHERE UserId = @userId
";


        public Dictionary<long, Domain.FlightEmailSubscriptions.SubscriptionDays> GetByUser(int userId)
        {
            Dictionary<long, Domain.FlightEmailSubscriptions.SubscriptionDays> result;

            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                var subscriptionDays = connection.Query<Domain.FlightEmailSubscriptions.SubscriptionDays>(GetByUserSql, new { userId = userId }).ToList();

                result = subscriptionDays.ToDictionary(s => s.FlightEmailSubscriptionId, s => s);
            }

            return result;
        }
    }
}

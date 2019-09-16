using System.Collections.Generic;
using System.Linq;
using Dapper;
using DreamTravel.Bot.DiscoverIndividualChances.Models;
using DreamTravel.Infrastructure.Database;

namespace DreamTravel.DatabaseData.Subscriptions
{
    public class SubscriptionRepository : ISubscriptionRepository
    {
        const string Sql = @"
SELECT 
    [User].[Name] AS UserName
    ,[From]
    ,[To]
    ,LengthOfStay
    ,[User].Email AS Email
    ,Currency
FROM Subscription
JOIN [User] on [User].Id = UserId
";

        public List<Subscription> GetSubscriptions()
        {
            List<Subscription> result;

            using (var connection = DbConnectionFactory.CreateConnection())
            {
                result = connection.Query<Subscription>(Sql).ToList();
            }

            return result;
        }
    }
}

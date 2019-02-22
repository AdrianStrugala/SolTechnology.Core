namespace DreamTravel.Bot.DiscoverIndividualChances.SendEmail
{
    using Dapper;
    using Interfaces;
    using Models;
    using System.Collections.Generic;
    using System.Linq;
    using Infrastructure.Database;

    public class ProvideSubscriptions : IProvideSubscriptions
    {
        const string Sql = @"
SELECT 
    [User].[Name] AS UserName
    ,[From]
    ,[To]
    ,NoOfMonthsFromNow
FROM Subscription
JOIN [User] on [User].Id = UserId
";

        public List<Subscription> Execute()
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

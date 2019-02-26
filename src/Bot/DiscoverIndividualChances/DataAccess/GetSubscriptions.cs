namespace DreamTravel.Bot.DiscoverIndividualChances.DataAccess
{
    using System.Collections.Generic;
    using System.Linq;
    using Dapper;
    using Infrastructure.Database;
    using Interfaces;
    using Models;

    public class GetSubscriptions : IGetSubscriptions
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

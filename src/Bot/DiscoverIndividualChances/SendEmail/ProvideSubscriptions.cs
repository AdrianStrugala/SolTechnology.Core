namespace DreamTravel.Bot.DiscoverIndividualChances.SendEmail
{
    using Configuration;
    using Dapper;
    using Interfaces;
    using Models;
    using System.Collections.Generic;
    using System.Linq;

    public class ProvideSubscriptions : IProvideSubscriptions
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        const string Sql = @"
SELECT 
    Name AS UserName
    ,From
    ,To
    ,NoOfMonthsFromNow
FROM Subscription
JOIN User on User.Id = UserId
";

        public ProvideSubscriptions(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public List<Subscription> Execute()
        {
            List<Subscription> result;

            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                result = connection.Query<Subscription>(Sql).ToList();
            }

            return result;
        }
    }
}

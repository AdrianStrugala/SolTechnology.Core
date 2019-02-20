namespace DreamTravel.Bot.DiscoverDreamTravelChances.SendEmail
{
    using System.Collections.Generic;
    using System.Linq;
    using Configuration;
    using Dapper;
    using Interfaces;
    using Models;

    public class ProvideRecipients : IProvideRecipients
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        private const string sql = @"
SELECT [Id]
      ,[Name]
      ,[Email]
  FROM [dbo].[User]
";

        public ProvideRecipients(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public List<User> Execute()
        {
            List<User> result;

            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                result = connection.Query<User>(sql).ToList();
            }
            
            return result;
        }
    }
}

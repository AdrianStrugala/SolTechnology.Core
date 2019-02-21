namespace DreamTravel.Bot.DiscoverDreamTravelChances.SendEmail
{
    using Dapper;
    using Interfaces;
    using Models;
    using System.Collections.Generic;
    using System.Linq;
    using Infrastructure.Database;

    public class ProvideRecipients : IProvideRecipients
    {
        private const string Sql = @"
SELECT [Id]
      ,[Name]
      ,[Email]
  FROM [dbo].[User]
";

        public List<User> Execute()
        {
            List<User> result;

            using (var connection = DbConnectionFactory.CreateConnection())
            {
                result = connection.Query<User>(Sql).ToList();
            }

            return result;
        }
    }
}

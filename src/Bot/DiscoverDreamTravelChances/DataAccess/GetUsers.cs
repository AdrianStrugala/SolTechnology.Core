namespace DreamTravel.Bot.DiscoverDreamTravelChances.DataAccess
{
    using System.Collections.Generic;
    using System.Linq;
    using Dapper;
    using Infrastructure.Database;
    using Interfaces;
    using Models;

    public class GetUsers : IGetUsers
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

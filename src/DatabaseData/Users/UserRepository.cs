using System.Collections.Generic;
using System.Linq;
using Dapper;
using DreamTravel.Domain.Users;
using DreamTravel.Infrastructure.Database;

namespace DreamTravel.DatabaseData.Users
{
    public class UserRepository : IUserRepository
    {
        private const string Sql = @"
SELECT [Id]
      ,[Name]
      ,[Email]
  FROM [dbo].[User]
";

        public List<User> GetUsers()
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

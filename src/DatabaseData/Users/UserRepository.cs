using System.Collections.Generic;
using System.Linq;
using Dapper;
using DreamTravel.Domain.Users;
using DreamTravel.Infrastructure.Database;

namespace DreamTravel.DatabaseData.Users
{
    public class UserRepository : IUserRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public UserRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        private const string Sql = @"
SELECT [Id]
      ,[Name]
      ,[Email]
  FROM [dbo].[User]
";

        public List<User> GetUsers()
        {
            List<User> result;

            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                result = connection.Query<User>(Sql).ToList();
            }

            return result;
        }
    }
}

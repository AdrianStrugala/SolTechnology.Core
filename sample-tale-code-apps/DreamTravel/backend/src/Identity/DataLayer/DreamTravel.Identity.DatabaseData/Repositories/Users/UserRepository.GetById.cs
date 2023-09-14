using System;
using System.Linq;
using Dapper;
using DreamTravel.Identity.Domain.Users;

namespace DreamTravel.Identity.DatabaseData.Repositories.Users
{
    public partial class UserRepository : IUserRepository
    {
        private const string GetByIdSql = @"
SELECT
      [UserId]
      ,[Name]
      ,[Password]
      ,[Email]
      ,[IsActive]
      ,[Currency]
  FROM [dbo].[User]
  WHERE [UserId] = @Id
";

        public User Get(Guid  id)
        {
            User result = null;
            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                result = connection.Query<User>(GetByIdSql, new
                {
                    Id = id
                }).FirstOrDefault();
            }

            return result;
        }
    }
}

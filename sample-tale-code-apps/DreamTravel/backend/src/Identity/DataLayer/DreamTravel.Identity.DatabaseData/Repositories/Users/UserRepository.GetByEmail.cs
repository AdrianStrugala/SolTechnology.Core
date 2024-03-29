﻿using System.Linq;
using Dapper;
using DreamTravel.Identity.Domain.Users;

namespace DreamTravel.Identity.DatabaseData.Repositories.Users
{
    public partial class UserRepository : IUserRepository
    {
        private const string GetSql = @"
SELECT [UserId]
      ,[Name]
      ,[Password]
      ,[Email]
      ,[IsActive]
      ,[Currency]
  FROM [dbo].[User]
  WHERE [Email] = @Email
";

        public User Get(string userEmail)
        {
            User result = null;
            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                result = connection.Query<User>(GetSql, new
                {
                    Email = userEmail
                }).FirstOrDefault();
            }

            return result;
        }
    }
}

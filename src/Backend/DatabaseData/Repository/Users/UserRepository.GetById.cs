using System.Linq;
using Dapper;
using DreamTravel.Domain.Users;

namespace DreamTravel.DatabaseData.Repository.Users
{
    public partial class UserRepository : IUserRepository
    {
        private const string GetByIdSql = @"
SELECT [Id]
      ,[Name]
      ,[Password]
      ,[Email]
      ,[IsActive]
      ,[Currency]
  FROM [dbo].[User]
  WHERE [Id] = @Id
";

        public User Get(int  id)
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

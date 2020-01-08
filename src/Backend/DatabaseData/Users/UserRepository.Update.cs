using Dapper;
using DreamTravel.Domain.Users;

namespace DreamTravel.DatabaseData.Users
{
    public partial class UserRepository : IUserRepository
    {
        private const string UpdateSql = @"
  UPDATE [dbo].[User]
  SET
    [Name] = @Name
      ,[Password] = @Password
      ,[Email] = @Email
      ,[IsActive] = @IsActive
WHERE
    [Id] = @Id
";

        public void Update(User user)
        {
            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                connection.Execute(UpdateSql, new
                {
                    Name = user.Name,
                    Password = user.Password,
                    Email = user.Email,
                    IsActive = true,
                    Id = user.Id
                });
            }
        }
    }
}

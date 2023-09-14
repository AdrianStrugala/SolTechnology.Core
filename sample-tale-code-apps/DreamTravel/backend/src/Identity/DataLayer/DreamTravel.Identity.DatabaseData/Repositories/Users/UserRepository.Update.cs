using Dapper;
using DreamTravel.Identity.Domain.Users;

namespace DreamTravel.Identity.DatabaseData.Repositories.Users
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
    [UserId] = @UserId
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
                    UserId = user.UserId
                });
            }
        }
    }
}

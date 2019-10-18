using Dapper;
using DreamTravel.Domain.Users;

namespace DreamTravel.DatabaseData.Users
{
    public partial class UserRepository : IUserRepository
    {
        private const string InsertSql = @"
  INSERT INTO [dbo].[User] (
        [Name]
      ,[Password]
      ,[Email]
      ,[IsActive])
VALUES (@Name, @Password, @Email, @IsActive)
";

        public void Insert(User user)
        {
            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                connection.Execute(InsertSql, new
                {
                    Name = user.Name,
                    Password = user.Password,
                    Email = user.Email,
                    IsActive = true
                });
            }
        }
    }
}

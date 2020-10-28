using Dapper;
using DreamTravel.Domain.Users;
using DreamTravel.Infrastructure.Database;

namespace DreamTravel.DatabaseData.Repository.Users
{
    public partial class UserRepository : IUserRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public UserRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }


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

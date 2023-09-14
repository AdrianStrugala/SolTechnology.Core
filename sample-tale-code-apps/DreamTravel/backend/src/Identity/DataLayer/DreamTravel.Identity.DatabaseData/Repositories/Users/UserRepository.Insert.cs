using Dapper;
using DreamTravel.Identity.Domain.Users;
using SolTechnology.Core.Sql.Connections;

namespace DreamTravel.Identity.DatabaseData.Repositories.Users
{
    public partial class UserRepository : IUserRepository
    {
        private readonly ISqlConnectionFactory _dbConnectionFactory;

        public UserRepository(ISqlConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }


        private const string InsertSql = @"
  INSERT INTO [dbo].[User] (
    [Name]
    ,[UserId]
    ,[Password]
    ,[Email]
    ,[IsActive])
VALUES (@Name, @UserId, @Password, @Email, @IsActive)
";

        public void Insert(User user)
        {
            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                connection.Execute(InsertSql, new
                {
                    Name = user.Name,
                    UserId = user.UserId,
                    Password = user.Password,
                    Email = user.Email,
                    IsActive = true
                });
            }
        }
    }
}

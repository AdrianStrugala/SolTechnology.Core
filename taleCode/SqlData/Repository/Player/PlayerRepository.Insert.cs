using Dapper;
using SolTechnology.Core.Sql.Connection;
using SolTechnology.TaleCode.Domain.Player;

namespace SolTechnology.TaleCode.SqlData.Repository.Player
{
    public partial class PlayerRepository : IPlayerRepository
    {
        private readonly ISqlConnectionFactory _sqlConnectionFactory;

        public PlayerRepository(ISqlConnectionFactory sqlConnectionFactory)
        {
            _sqlConnectionFactory = sqlConnectionFactory;
        }


        private const string InsertSql = @"
  INSERT INTO [dbo].[Player] (
    [ApiId],
	[Name],
	[DateOfBirth],
    [Nationality],
	[Position])
VALUES (@ApiId, @Name, @DateOfBirth, @Nationality, @Position)
";

        public void Insert(Domain.Player.Player player)
        {
            using (var connection = _sqlConnectionFactory.CreateConnection())
            {
                connection.Execute(InsertSql, new
                {
                    ApiId = player.ApiId,
                    Name = player.Name,
                    DateOfBirth = player.DateOfBirth,
                    Nationality = player.Nationality,
                    Position = player.Position
                });
            }
        }
    }
}

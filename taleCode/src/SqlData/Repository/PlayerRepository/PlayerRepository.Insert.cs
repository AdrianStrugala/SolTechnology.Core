using Dapper;
using DapperExtensions;
using SolTechnology.Core.Sql.Connection;
using SolTechnology.TaleCode.Domain;

namespace SolTechnology.TaleCode.SqlData.Repository.PlayerRepository
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

        public void Insert(Player player)
        {
            var parameters = new
            {
                ApiId = player.ApiId,
                Name = player.Name,
                DateOfBirth = player.DateOfBirth,
                Nationality = player.Nationality,
                Position = player.Position
            };

            if (_sqlConnectionFactory.HasOpenTransaction)
            {
                var transaction = _sqlConnectionFactory.GetTransaction();
                transaction.Connection.Execute(InsertSql, parameters, transaction);
                transaction.Connection.Insert<Team>(player.Teams, transaction);
            }
            else
            {
                using (var connection = _sqlConnectionFactory.CreateConnection())
                {
                    using (var transaction = connection.BeginTransaction())
                    {
                        connection.Execute(InsertSql, parameters, transaction);
                        connection.Insert<Team>(player.Teams, transaction);

                        transaction.Commit();
                    }
                }
            }
        }
    }
}

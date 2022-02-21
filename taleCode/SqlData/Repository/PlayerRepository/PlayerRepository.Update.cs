using Dapper;
using SolTechnology.Core.Sql.Connection;
using SolTechnology.TaleCode.Domain;

namespace SolTechnology.TaleCode.SqlData.Repository.PlayerRepository
{
    public partial class PlayerRepository : IPlayerRepository
    {
        private const string UpdateSql = @"

TODO

";

        public void Update(Player player)
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

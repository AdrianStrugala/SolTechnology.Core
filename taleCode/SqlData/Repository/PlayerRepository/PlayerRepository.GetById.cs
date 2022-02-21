using Dapper;
using SolTechnology.TaleCode.Domain;

namespace SolTechnology.TaleCode.SqlData.Repository.PlayerRepository
{
    public partial class PlayerRepository : IPlayerRepository
    {

        private const string GetByIdSql = @"
  SELECT
    [ApiId],
	[Name],
	[DateOfBirth],
    [Nationality],
	[Position]
FROM [dbo].[Player]
WHERE [ApiId] = @PlayerApiId
";

        public Player GetById(int apiId)
        {
            Player result = null;

            using (var connection = _sqlConnectionFactory.CreateConnection())
            {
                result = connection.QuerySingleOrDefault<Player>(GetByIdSql, new
                {
                    PlayerApiId = apiId,
                });
            }

            return result;
        }
    }
}

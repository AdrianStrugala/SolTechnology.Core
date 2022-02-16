using Dapper;
using SolTechnology.TaleCode.Domain;

namespace SolTechnology.TaleCode.SqlData.Repository.MatchRepository
{
    public partial class MatchRepository : IMatchRepository
    {

        private const string GetByPlayerIdSql = @"
  SELECT (
    [ApiId],
    [PlayerApiId],
    [Date],
	[HomeTeam],
	[AwayTeam],
    [HomeTeamScore],
    [AwayTeamScore],
    [Winner],
	[CompetitionWinner])
FROM [dbo].[Match]
WHERE [PlayerApiId] = @PlayerApiId
";

        public List<Match> GetByPlayerId(int playerApiId)
        {
            List<Match> result = null;

            using (var connection = _sqlConnectionFactory.CreateConnection())
            {
                result = connection.Query<Match>(GetByPlayerIdSql, new
                {
                    PlayerApiId = playerApiId,
                }).ToList();
            }

            return result;
        }
    }
}

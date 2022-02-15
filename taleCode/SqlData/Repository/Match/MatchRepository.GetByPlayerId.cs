using Dapper;
using SolTechnology.TaleCode.Domain.Match;

namespace SolTechnology.TaleCode.SqlData.Repository.Match
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

        public List<Domain.Match.Match> GetByPlayerId(int playerApiId)
        {
            List<Domain.Match.Match> result = null;

            using (var connection = _sqlConnectionFactory.CreateConnection())
            {
                result = connection.Query<Domain.Match.Match>(GetByPlayerIdSql, new
                {
                    PlayerApiId = playerApiId,
                }).ToList();
            }

            return result;
        }
    }
}

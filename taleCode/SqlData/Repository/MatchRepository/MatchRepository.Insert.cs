using Dapper;
using SolTechnology.Core.Sql.Connection;
using SolTechnology.TaleCode.Domain;

namespace SolTechnology.TaleCode.SqlData.Repository.MatchRepository
{
    public partial class MatchRepository
    {
        private readonly ISqlConnectionFactory _sqlConnectionFactory;

        public MatchRepository(ISqlConnectionFactory sqlConnectionFactory)
        {
            _sqlConnectionFactory = sqlConnectionFactory;
        }


        private const string InsertSql = @"
  INSERT INTO [dbo].[Match] (
    [ApiId],
    [PlayerApiId],
    [Date],
	[HomeTeam],
	[AwayTeam],
    [HomeTeamScore],
    [AwayTeamScore],
    [Winner],
	[CompetitionWinner])
VALUES (@ApiId, @PlayerApiId, @Date, @HomeTeam, @AwayTeam, @HomeTeamScore, @AwayTeamScore, @Winner, @CompetitionWinner)
";

        public void Insert(Match match)
        {
            using (var connection = _sqlConnectionFactory.CreateConnection())
            {
                connection.Execute(InsertSql, new
                {
                    ApiId = match.ApiId,
                    PlayerApiId = match.PlayerApiId,
                    Date = match.Date,
                    HomeTeam = match.HomeTeam,
                    AwayTeam = match.AwayTeam,
                    HomeTeamScore = match.HomeTeamScore,
                    AwayTeamScore = match.AwayTeamScore,
                    Winner = match.Winner,
                    CompetitionWinner = match.CompetitionWinner
                });
            }
        }
    }
}

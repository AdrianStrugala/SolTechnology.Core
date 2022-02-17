using SolTechnology.Core.Guards;

namespace SolTechnology.TaleCode.Domain
{
    public record Match
    {
        public int ApiId { get; set; }
        public int PlayerApiId { get; set; }
        public DateTime Date { get; set; }
        public string HomeTeam { get; set; }
        public string AwayTeam { get; set; }
        public int HomeTeamScore { get; set; }
        public int AwayTeamScore { get; set; }
        public string Winner { get; set; }
        public string CompetitionWinner { get; set; }

        public Match(int apiId, int playerApiId, DateTime date, string homeTeam, string awayTeam, int homeTeamScore, int awayTeamScore, string winner, string competitionWinner)
        {
            Guards.StringNotNullNorEmpty(winner, nameof(winner));
            Guards.StringNotNullNorEmpty(competitionWinner, nameof(competitionWinner));
            Guards.IntNotZero(apiId, nameof(apiId));
            Guards.IntNotZero(playerApiId, nameof(playerApiId));

            ApiId = apiId;
            PlayerApiId = playerApiId;
            Date = date;
            HomeTeam = homeTeam;
            AwayTeam = awayTeam;
            HomeTeamScore = homeTeamScore;
            AwayTeamScore = awayTeamScore;
            Winner = winner;
            CompetitionWinner = competitionWinner;
        }
    }
}

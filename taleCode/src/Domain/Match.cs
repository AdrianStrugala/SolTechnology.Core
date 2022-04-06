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

        private Match()
        {
            //required by ORM
        }

        public Match(int apiId, int playerApiId, DateTime date, string homeTeam, string awayTeam, int homeTeamScore, int awayTeamScore, string winner)
        {
            Guards.String(winner, nameof(winner)).NotNull().NotEmpty();
            Guards.Int(apiId, nameof(apiId)).NotZero();
            Guards.Int(playerApiId, nameof(playerApiId)).NotZero();

            ApiId = apiId;
            PlayerApiId = playerApiId;
            Date = date;
            HomeTeam = homeTeam;
            AwayTeam = awayTeam;
            HomeTeamScore = homeTeamScore;
            AwayTeamScore = awayTeamScore;
            Winner = winner;
        }

        public void AssignCompetitionWinner(string competitionWinner)
        {
            Guards.String(competitionWinner, nameof(competitionWinner)).NotNull().NotEmpty();
            CompetitionWinner = competitionWinner;
        }
    }
}

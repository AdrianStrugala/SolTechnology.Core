namespace ApiClients.FootballDataApi.Models
{
    public class FootballDataMatch
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string HomeTeam { get; set; }
        public string AwayTeam { get; set; }
        public int HomeTeamScore { get; set; }
        public int AwayTeamScore { get; set; }
        public string Winner { get; set; }
        public string CompetitionWinner { get; set; }
    }
}

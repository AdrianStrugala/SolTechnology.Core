namespace ApiClients.FootballDataApi.Models;

public class Match
{
    public int Id { get; set; }
    public Competition Competition { get; set; }
    public Season Season { get; set; }
    public DateTime UtcDate { get; set; }
    public string Status { get; set; }
    public string Venue { get; set; }
    public int? Matchday { get; set; }
    public string Stage { get; set; }
    public object Group { get; set; }
    public DateTime LastUpdated { get; set; }
    public Odds Odds { get; set; }
    public Score Score { get; set; }
    public HomeTeam HomeTeam { get; set; }
    public AwayTeam AwayTeam { get; set; }
    public List<Referee> Referees { get; set; }
}
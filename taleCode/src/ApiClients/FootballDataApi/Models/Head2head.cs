namespace ApiClients.FootballDataApi.Models;

public class Head2head
{
    public int NumberOfMatches { get; set; }
    public int TotalGoals { get; set; }
    public HomeTeam HomeTeam { get; set; }
    public AwayTeam AwayTeam { get; set; }
}
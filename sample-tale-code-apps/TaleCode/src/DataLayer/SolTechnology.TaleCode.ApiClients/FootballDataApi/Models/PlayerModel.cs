namespace SolTechnology.TaleCode.ApiClients.FootballDataApi.Models;

public class PlayerModel
{
    public int Count { get; set; }
    public Filters Filters { get; set; }
    public Player Player { get; set; }
    public List<Match> Matches { get; set; }
}

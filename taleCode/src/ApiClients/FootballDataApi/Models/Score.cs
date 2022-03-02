namespace ApiClients.FootballDataApi.Models;

public class Score
{
    public string Winner { get; set; }
    public string Duration { get; set; }
    public FullTime FullTime { get; set; }
    public HalfTime HalfTime { get; set; }
    public ExtraTime ExtraTime { get; set; }
    public Penalties Penalties { get; set; }
}
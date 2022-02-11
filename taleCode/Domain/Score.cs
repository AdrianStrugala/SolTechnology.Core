namespace SolTechnology.TaleCode.Domain;

public record Score
{
    public string winner { get; set; }
    public string duration { get; set; }
    public TimeScore fullTime { get; set; }
    public TimeScore halfTime { get; set; }
    public TimeScore extraTime { get; set; }
    public TimeScore penalties { get; set; }
}

public class TimeScore
{
    public int homeTeam { get; set; }
    public int awayTeam { get; set; }
}

namespace SolTechnology.TaleCode.Domain
{
    public record Match
    {
        public int id { get; set; }
        public Competition Competition { get; set; }
        public DateTime UtcDate { get; set; }
        public string status { get; set; }
        public int matchday { get; set; }
        public string stage { get; set; }
        public string group { get; set; }
        public DateTime lastUpdated { get; set; }
        public Score score { get; set; }
        public Team homeTeam { get; set; }
        public Team awayTeam { get; set; }
    }

    public class Competition
    {
        public int id { get; set; }
        public string name { get; set; }
    }
}

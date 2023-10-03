namespace SolTechnology.TaleCode.PlayerRegistry.Queries.GetPlayerStatistics
{
    public class GetPlayerStatisticsResult
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int NumberOfMatches { get; set; }
        public List<StatisticsByTeam> StatisticsByTeams { get; set; } = new();

    }

    public class StatisticsByTeam
    {
        public string TeamName { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public int NumberOfMatches { get; set; }
        public decimal PercentageOfMatchesResultingCompetitionVictory { get; set; }
    }
}
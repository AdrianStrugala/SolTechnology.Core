namespace SolTechnology.TaleCode.PlayerRegistry.Commands.CalculatePlayerStatistics
{
    public class PlayerStatistics
    {
        public string Name { get; set; }
        public int NumberOfMatches { get; set; }
        public List<StatisticsByTeam> StatisticsByTeams { get; set; }

    }

    public class StatisticsByTeam
    {
        public string TeamName { get; set; }
        public DateOnly DateFrom { get; set; }
        public DateOnly DateTo { get; set; }
        public int NumberOfMatches { get; set; }
        public decimal PercentageOfMatchesResultingCompetitionVictory { get; set; }
    }
}

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.CalculatePlayerStatistics
{
    public class CalculatePlayerStatisticsHandler
    {
        public PlayerStatistics Handle(CalculatePlayerStatisticsCommand command)
        {
            var result = new PlayerStatistics();

            return result;
        }
    }

    public class CalculatePlayerStatisticsCommand
    {
        public string PlayerName { get; set; }
    }
}

using SolTechnology.Core.Guards;

namespace SolTechnology.TaleCode.PlayerRegistry.Queries.GetPlayerStatistics
{
    public class GetPlayerStatisticsQuery
    {
        public int PlayerId { get; set; }

        public GetPlayerStatisticsQuery(int playerId)
        {
            var guards = new Guards();
            guards.Int(playerId, nameof(playerId), x => x.NotNegative().NotZero()).ThrowOnError();

            PlayerId = playerId;
        }
    }
}
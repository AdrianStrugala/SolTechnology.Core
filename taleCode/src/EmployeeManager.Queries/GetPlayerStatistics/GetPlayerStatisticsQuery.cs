using SolTechnology.Core.Guards;
using SolTechnology.TaleCode.Infrastructure;

namespace SolTechnology.TaleCode.PlayerRegistry.Queries.GetPlayerStatistics
{
    public class GetPlayerStatisticsQuery : IQuery
    {
        public int PlayerId { get; set; }

        public GetPlayerStatisticsQuery(int playerId)
        {
            Guards.Int(playerId, nameof(playerId)).NotNegative();

            PlayerId = playerId;
        }
    }
}
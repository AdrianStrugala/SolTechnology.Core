using SolTechnology.TaleCode.Domain;

namespace SolTechnology.TaleCode.BlobData.PlayerStatisticsRepository;

public interface IPlayerStatisticsRepository
{
    Task Add(PlayerStatistics playerStatistics);

    Task<PlayerStatistics> Get(int playerId);
}
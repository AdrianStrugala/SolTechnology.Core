using SolTechnology.TaleCode.Domain;
using SolTechnology.TaleCode.StaticData.PlayerId;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Interfaces;

public interface ISyncPlayer
{
    Task<Player> Execute(PlayerIdMap playerIdMap);
}
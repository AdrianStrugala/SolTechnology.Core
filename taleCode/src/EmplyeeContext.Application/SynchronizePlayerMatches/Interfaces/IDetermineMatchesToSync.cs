using SolTechnology.TaleCode.Domain;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Interfaces;

public interface IDetermineMatchesToSync
{
    List<int> Execute(Player player);
}
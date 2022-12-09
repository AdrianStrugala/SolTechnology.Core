namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Interfaces;

public interface ISyncMatch
{
    Task Execute(int playerId, int matchId);
}
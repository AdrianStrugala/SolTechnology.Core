namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Interfaces;

public interface ISyncMatch
{
    Task Execute(SynchronizePlayerMatchesContext context, int matchId);
}
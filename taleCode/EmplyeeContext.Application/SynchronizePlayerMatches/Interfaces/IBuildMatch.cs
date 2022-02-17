using SolTechnology.TaleCode.Domain;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Interfaces;

public interface IBuildMatch
{
    Task<Match> Execute(int playerId, int matchId);
}
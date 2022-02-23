using SolTechnology.TaleCode.Domain;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Interfaces;

public interface ISyncPlayer
{
    Task Execute(SynchronizePlayerMatchesContext context);
}
using SolTechnology.TaleCode.Infrastructure;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches
{
    public class SynchronizePlayerMatchesCommand : ICommand
    {
        public int PlayerId { get; set; }

        public SynchronizePlayerMatchesCommand(int playerId)
        {
            PlayerId = playerId;
        }
    }
}
using SolTechnology.Core.MessageBus;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches
{
    public class PlayerMatchesSynchronizedEvent : IMessage
    {
        public int PlayerId { get; set; }

        public PlayerMatchesSynchronizedEvent(int playerId)
        {
            PlayerId = playerId;
        }
    }
}

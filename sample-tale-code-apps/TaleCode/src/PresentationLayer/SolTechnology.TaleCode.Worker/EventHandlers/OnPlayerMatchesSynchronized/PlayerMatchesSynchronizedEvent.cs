using SolTechnology.Core.MessageBus;

namespace SolTechnology.TaleCode.Worker.EventHandlers.OnPlayerMatchesSynchronized
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

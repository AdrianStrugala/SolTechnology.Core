using SolTechnology.Core.MessageBus;
using SolTechnology.Core.MessageBus.Publish;

namespace SolTechnology.TaleCode.EventListener.PlayerMatchesSynchronized
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

using SolTechnology.Core.MessageBus;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches
{
    public class PlayerMatchesSynchronizedEvent : IMessage
    {
        public string MessageType => "PlayerMatchesSynchronizedEvent";

        public string PlayerName { get; set; }

        public PlayerMatchesSynchronizedEvent(string playerName)
        {
            PlayerName = playerName;
        }
    }
}

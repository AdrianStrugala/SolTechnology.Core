using SolTechnology.TaleCode.Infrastructure;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches
{
    public class SynchronizePlayerMatchesCommand : ICommand
    {
        public string PlayerName { get; set; }

        public SynchronizePlayerMatchesCommand(string playerName)
        {
            PlayerName = playerName;
        }

        string ICommand.CommandId => PlayerName;
        string ICommand.CommandName => nameof(SynchronizePlayerMatches);
    }
}
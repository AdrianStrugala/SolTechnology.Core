using SolTechnology.Core.Logging;
using SolTechnology.TaleCode.Infrastructure;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches
{
    public class SynchronizePlayerMatchesCommand : ICommand, ILoggedOperation
    {
        public string PlayerName { get; set; }

        public SynchronizePlayerMatchesCommand(string playerName)
        {
            PlayerName = playerName;
        }

        LogScope ILoggedOperation.LogScope => new LogScope
        {
            OperationId = PlayerName,
            OperationIdName = nameof(PlayerName),
            OperationName = nameof(SynchronizePlayerMatches)
        };
    }
}
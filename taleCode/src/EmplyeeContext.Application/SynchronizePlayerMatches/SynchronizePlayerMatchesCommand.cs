using SolTechnology.Core.Logging;
using SolTechnology.TaleCode.Infrastructure;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches
{
    public class SynchronizePlayerMatchesCommand : ICommand, ILoggedOperation
    {
        public int PlayerId { get; set; }

        public SynchronizePlayerMatchesCommand(int playerId)
        {
            PlayerId = playerId;
        }

        LogScope ILoggedOperation.LogScope => new()
        {
            OperationId = PlayerId,
            OperationIdName = nameof(PlayerId),
            OperationName = nameof(SynchronizePlayerMatches)
        };
    }
}
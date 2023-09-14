using System.Windows.Input;
using SolTechnology.Core.Logging;
using SolTechnology.TaleCode.Infrastructure;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.CalculatePlayerStatistics;

public class CalculatePlayerStatisticsCommand : ILoggedOperation
{
    public int PlayerId { get; set; }

    public CalculatePlayerStatisticsCommand(int playerId)
    {
        PlayerId = playerId;
    }


    LogScope ILoggedOperation.LogScope => new LogScope
    {
        OperationId = PlayerId,
        OperationIdName = nameof(PlayerId),
        OperationName = nameof(CalculatePlayerStatistics)
    };
}
using SolTechnology.Core.Logging;
using SolTechnology.TaleCode.Infrastructure;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.CalculatePlayerStatistics;

public class CalculatePlayerStatisticsCommand : ICommand, ILoggedOperation
{
    public string PlayerName { get; set; }

    LogScope ILoggedOperation.LogScope => new LogScope
    {
        OperationId = PlayerName,
        OperationIdName = nameof(PlayerName),
        OperationName = nameof(CalculatePlayerStatistics)
    };
}
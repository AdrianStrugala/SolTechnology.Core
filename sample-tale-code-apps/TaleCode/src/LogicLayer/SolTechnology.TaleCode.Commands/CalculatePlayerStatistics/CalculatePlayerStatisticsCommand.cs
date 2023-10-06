using FluentValidation;
using SolTechnology.Core.Logging;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.CalculatePlayerStatistics;

public class CalculatePlayerStatisticsCommand : ILoggableOperation
{
    public int PlayerId { get; set; }

    public CalculatePlayerStatisticsCommand(int playerId)
    {
        PlayerId = playerId;
    }


    LogScope ILoggableOperation.LogScope => new()
    {
        OperationId = PlayerId,
        OperationIdName = nameof(PlayerId),
        OperationName = nameof(CalculatePlayerStatistics)
    };
}

public class CalculatePlayerStatisticsCommandValidator : AbstractValidator<CalculatePlayerStatisticsCommand>
{
    public CalculatePlayerStatisticsCommandValidator()
    {
        RuleFor(x => x.PlayerId)
            .NotNull()
            .NotEmpty()
            .GreaterThan(0);
    }
}
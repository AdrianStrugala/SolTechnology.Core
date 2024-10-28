using FluentValidation;
using MediatR;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Logging;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.CalculatePlayerStatistics;

public class CalculatePlayerStatisticsCommand : ILoggableOperation, IRequest<Result>, IRequest
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
using FluentValidation;
using SolTechnology.Core.Guards;
using SolTechnology.Core.Logging;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches;

public class SynchronizePlayerMatchesCommand : ILoggableOperation
{
    public int PlayerId { get; set; }

    public SynchronizePlayerMatchesCommand(int playerId)
    {
        var guards = new Guards();
        guards.Int(playerId, nameof(playerId), x => x.NotNegative().NotZero()).ThrowOnError();

        PlayerId = playerId;
    }

    LogScope ILoggableOperation.LogScope => new()
    {
        OperationId = PlayerId,
        OperationIdName = nameof(PlayerId),
        OperationName = nameof(SynchronizePlayerMatches)
    };
}

public class SynchronizePlayerMatchesCommandValidator : AbstractValidator<SynchronizePlayerMatchesCommand>
{
    public SynchronizePlayerMatchesCommandValidator()
    {
        RuleFor(x => x.PlayerId)
            .Equal(44)
            .WithMessage("Only Cristiano Ronaldo (Id 44) sync allowed. Noob");
    }
}
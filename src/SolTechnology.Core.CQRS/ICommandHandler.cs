namespace SolTechnology.Core.CQRS;

/// <summary>
/// Handles a command that produces no meaningful data (side-effect only).
/// </summary>
public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task<Result> Handle(TCommand command, CancellationToken cancellationToken);
}

/// <summary>
/// Handles a command that produces a result of type <typeparamref name="TResult"/>.
/// </summary>
public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<Result<TResult>> Handle(TCommand command, CancellationToken cancellationToken);
}

namespace SolTechnology.Core.CQRS;

/// <summary>
/// Handles a command that produces no meaningful data (side-effect only).
/// </summary>
public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    /// <summary>Executes the command; returns a failed <see cref="Result"/> for business failures instead of throwing.</summary>
    Task<Result> Handle(TCommand command, CancellationToken cancellationToken);
}

/// <summary>
/// Handles a command that produces a result of type <typeparamref name="TResult"/>.
/// </summary>
public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    /// <summary>Executes the command and returns its data, or a failed <see cref="Result{TResult}"/> for business failures.</summary>
    Task<Result<TResult>> Handle(TCommand command, CancellationToken cancellationToken);
}

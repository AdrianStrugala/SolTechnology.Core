namespace SolTechnology.Core.CQRS
{
    public interface ICommandHandler<in TCommand>
    {
        public Task<OperationResult> Handle(TCommand command, CancellationToken cancellationToken = default);
    }

    public interface ICommandHandler<in TCommand, TResult>
    {
        public Task<OperationResult<TResult>> Handle(TCommand command, CancellationToken cancellationToken = default);
    }
}
namespace SolTechnology.Core.CQRS
{
    public interface ICommandHandler<in TCommand>
    {
        public Task<Result> Handle(TCommand command, CancellationToken cancellationToken = default);
    }

    public interface ICommandHandler<in TCommand, TResult>
    {
        public Task<Result<TResult>> Handle(TCommand command, CancellationToken cancellationToken = default);
    }
}
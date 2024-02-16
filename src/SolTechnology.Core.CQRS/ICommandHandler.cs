namespace SolTechnology.Core.CQRS
{
    public interface ICommandHandler<in TCommand>
    {
        public Task<OperationResult> Handle(TCommand command);
    }

    public interface ICommandHandler<in TCommand, TResult>
    {
        public Task<OperationResult<TResult>> Handle(TCommand command);
    }
}
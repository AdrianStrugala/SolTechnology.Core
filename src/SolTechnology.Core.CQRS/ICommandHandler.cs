namespace SolTechnology.Core.CQRS
{
    public interface ICommandHandler<in TCommand>
    {
        public Task Handle(TCommand command);
    }

    public interface ICommandHandler<in TCommand, TResult>
    {
        public Task<TResult> Handle(TCommand command);
    }
}
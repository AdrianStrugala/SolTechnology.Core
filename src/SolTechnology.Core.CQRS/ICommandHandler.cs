namespace SolTechnology.Core.CQRS
{
    public interface ICommandHandler<in TCommand>
    {
        public Task<CommandResult> Handle(TCommand command);
    }

    public interface ICommandHandler<in TCommand, TResult>
    {
        public Task<CommandResult<TResult>> Handle(TCommand command);
    }
}
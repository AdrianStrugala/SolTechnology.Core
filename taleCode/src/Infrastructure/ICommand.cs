namespace SolTechnology.TaleCode.Infrastructure
{
    public interface ICommand
    {
        public string CommandId { get; }
        public string CommandName { get; }
    }

    public interface ICommandHandler<in TCommand> where TCommand : ICommand
    {
        public Task Handle(TCommand command);
    }
}
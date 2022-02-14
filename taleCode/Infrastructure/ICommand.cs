namespace SolTechnology.TaleCode.Infrastructure
{
    public interface ICommand
    {
    }

    public interface ICommandHandler<in TCommand> where TCommand : ICommand
    {
        public Task Handle(TCommand command);
    }
}
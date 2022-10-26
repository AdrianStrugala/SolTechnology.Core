namespace SolTechnology.Core.CQRS
{
    public interface ICommandHandler<in TCommand>
    {
        public Task Handle(TCommand command);
    }
}
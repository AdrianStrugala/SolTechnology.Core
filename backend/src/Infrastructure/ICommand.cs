namespace DreamTravel.Infrastructure
{
    public interface ICommandHandler<in TCommand> 
    {
        public CommandResult Handle(TCommand command);
    }

    public class CommandResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}

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

        public static CommandResult Succeeded()
        {
            return new CommandResult
            {
                Success = true
            };
        }

        public static CommandResult Failed(string message)
        {
            return new CommandResult
            {
                Message = message,
                Success = false
            };
        }
    }

}

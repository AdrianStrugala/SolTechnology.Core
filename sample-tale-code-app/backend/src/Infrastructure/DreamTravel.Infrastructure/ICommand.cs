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

// add mediatr? Keep commandResult and commandResult<T> 
// something similar for executors? they should get context, not sure about operation result -> Cool check on result (if failed, then rewrite failed message)



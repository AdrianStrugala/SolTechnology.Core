namespace SolTechnology.Core.CQRS
{
    public class CommandResult
    {
        public bool IsSuccess { get; init; }
        public bool IsFailure => !IsSuccess;
        public string ErrorMessage { get; init; }

        public static CommandResult Succeeded()
        {
            return new CommandResult
            {
                IsSuccess = true
            };
        }

        public static Task<CommandResult> SucceededTask()
        {
            return Task.FromResult(new CommandResult
            {
                IsSuccess = true
            });
        }

        public static CommandResult Failed(string message)
        {
            return new CommandResult
            {
                ErrorMessage = message,
                IsSuccess = false
            };
        }

        public static Task<CommandResult> FailedTask(string message)
        {
            return Task.FromResult(new CommandResult
            {
                ErrorMessage = message,
                IsSuccess = false
            });
        }
    }

    public class CommandResult<T> : CommandResult
    {
        public T Data { get; set; }


        public static CommandResult<T> Succeeded(T data)
        {
            return new CommandResult<T>
            {
                IsSuccess = true,
                Data = data
            };
        }

        public new static CommandResult<T> Failed(string message)
        {
            return new CommandResult<T>
            {
                ErrorMessage = message,
                IsSuccess = false
            };
        }

        public new static Task<CommandResult<T>> FailedTask(string message)
        {
            return Task.FromResult(new CommandResult<T>
            {
                ErrorMessage = message,
                IsSuccess = false
            });
        }

        public static Task<CommandResult<T>> SucceededTask(T data)
        {
            return Task.FromResult(new CommandResult<T>
            {
                IsSuccess = true,
                Data = data
            });
        }
    }
}

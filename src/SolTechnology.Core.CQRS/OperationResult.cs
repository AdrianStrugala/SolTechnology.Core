namespace SolTechnology.Core.CQRS
{
    public record OperationResult
    {
        public bool IsSuccess { get; init; }
        public bool IsFailure => !IsSuccess;
        public string ErrorMessage { get; init; }

        public static OperationResult Succeeded()
        {
            return new OperationResult
            {
                IsSuccess = true
            };
        }

        public static Task<OperationResult> SucceededTask()
        {
            return Task.FromResult(new OperationResult
            {
                IsSuccess = true
            });
        }

        public static OperationResult Failed(string message)
        {
            return new OperationResult
            {
                ErrorMessage = message,
                IsSuccess = false
            };
        }

        public static Task<OperationResult> FailedTask(string message)
        {
            return Task.FromResult(new OperationResult
            {
                ErrorMessage = message,
                IsSuccess = false
            });
        }
    }

    public record OperationResult<T> : OperationResult
    {
        public T Data { get; set; }


        public static OperationResult<T> Succeeded(T data)
        {
            return new OperationResult<T>
            {
                IsSuccess = true,
                Data = data
            };
        }

        public new static OperationResult<T> Failed(string message)
        {
            return new OperationResult<T>
            {
                ErrorMessage = message,
                IsSuccess = false
            };
        }

        public new static Task<OperationResult<T>> FailedTask(string message)
        {
            return Task.FromResult(new OperationResult<T>
            {
                ErrorMessage = message,
                IsSuccess = false
            });
        }

        public static Task<OperationResult<T>> SucceededTask(T data)
        {
            return Task.FromResult(new OperationResult<T>
            {
                IsSuccess = true,
                Data = data
            });
        }
    }
}

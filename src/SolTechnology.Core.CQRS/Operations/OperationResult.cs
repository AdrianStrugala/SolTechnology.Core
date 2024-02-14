namespace SolTechnology.Core.CQRS.Operations
{
    public class OperationResult
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
}


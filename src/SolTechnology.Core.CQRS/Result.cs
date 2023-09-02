namespace SolTechnology.Core.CQRS
{
    public class Result
    {
        public bool IsSuccess { get; init; }
        public bool IsFailure => !IsSuccess;
        public string ErrorMessage { get; init; }

        public static Result Succeeded()
        {
            return new Result
            {
                IsSuccess = true
            };
        }

        public static Task<Result> SucceededTask()
        {
            return Task.FromResult(new Result
            {
                IsSuccess = true
            });
        }

        public static Result Failed(string message)
        {
            return new Result
            {
                ErrorMessage = message,
                IsSuccess = false
            };
        }

        public static Task<Result> FailedTask(string message)
        {
            return Task.FromResult(new Result
            {
                ErrorMessage = message,
                IsSuccess = false
            });
        }
    }

    public class Result<T> : Result
    {
        public T Data { get; set; }


        public static Result<T> Succeeded(T data)
        {
            return new Result<T>
            {
                IsSuccess = true,
                Data = data
            };
        }

        public new static Result<T> Failed(string message)
        {
            return new Result<T>
            {
                ErrorMessage = message,
                IsSuccess = false
            };
        }

        public new static Task<Result<T>> FailedTask(string message)
        {
            return Task.FromResult(new Result<T>
            {
                ErrorMessage = message,
                IsSuccess = false
            });
        }

        public static Task<Result<T>> SucceededTask(T data)
        {
            return Task.FromResult(new Result<T>
            {
                IsSuccess = true,
                Data = data
            });
        }
    }
}

namespace SolTechnology.Core.CQRS
{
    public record Result
    {
        public bool IsSuccess { get; init; }
        public bool IsFailure => !IsSuccess;
        public string ErrorMessage { get; init; }


        public static Result Success()
        {
            return new Result
            {
                IsSuccess = true
            };
        }

        public static Task<Result> SuccessAsTask()
        {
            return Task.FromResult(new Result
            {
                IsSuccess = true
            });
        }

        public static Result Fail(string message)
        {
            return new Result
            {
                ErrorMessage = message,
                IsSuccess = false
            };
        }

        public static Task<Result> FailAsTask(string message)
        {
            return Task.FromResult(new Result
            {
                ErrorMessage = message,
                IsSuccess = false
            });
        }
    }

    public record Result<T> : Result
    {
        public T Data { get; set; }

        public static implicit operator Result<T>(T value)
        {
            return Success(value);
        }

        public static implicit operator Result<T>(Exception e)
        {
            return Fail(e.Message);
        }

        public static Result<T> Success(T data)
        {
            return new Result<T>
            {
                IsSuccess = true,
                Data = data
            };
        }

        public new static Result<T> Fail(string message)
        {
            return new Result<T>
            {
                ErrorMessage = message,
                IsSuccess = false
            };
        }

        public new static Task<Result<T>> FailAsTask(string message)
        {
            return Task.FromResult(new Result<T>
            {
                ErrorMessage = message,
                IsSuccess = false
            });
        }

        public static Task<Result<T>> SuccessAsTask(T data)
        {
            return Task.FromResult(new Result<T>
            {
                IsSuccess = true,
                Data = data
            });
        }
    }
}

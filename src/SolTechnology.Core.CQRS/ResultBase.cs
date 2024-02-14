namespace SolTechnology.Core.CQRS
{
    public class ResultBase
    {
        public bool IsSuccess { get; init; }
        public bool IsFailure => !IsSuccess;
        public string ErrorMessage { get; init; }

        public static ResultBase Succeeded()
        {
            return new ResultBase
            {
                IsSuccess = true
            };
        }

        public static Task<ResultBase> SucceededTask()
        {
            return Task.FromResult(new ResultBase
            {
                IsSuccess = true
            });
        }

        public static ResultBase Failed(string message)
        {
            return new ResultBase
            {
                ErrorMessage = message,
                IsSuccess = false
            };
        }

        public static Task<ResultBase> FailedTask(string message)
        {
            return Task.FromResult(new ResultBase
            {
                ErrorMessage = message,
                IsSuccess = false
            });
        }
    }

    public class ResultBase<T> : ResultBase
    {
        public T Data { get; set; }


        public static ResultBase<T> Succeeded(T data)
        {
            return new ResultBase<T>
            {
                IsSuccess = true,
                Data = data
            };
        }

        public new static ResultBase<T> Failed(string message)
        {
            return new ResultBase<T>
            {
                ErrorMessage = message,
                IsSuccess = false
            };
        }

        public new static Task<ResultBase<T>> FailedTask(string message)
        {
            return Task.FromResult(new ResultBase<T>
            {
                ErrorMessage = message,
                IsSuccess = false
            });
        }

        public static Task<ResultBase<T>> SucceededTask(T data)
        {
            return Task.FromResult(new ResultBase<T>
            {
                IsSuccess = true,
                Data = data
            });
        }
    }
}

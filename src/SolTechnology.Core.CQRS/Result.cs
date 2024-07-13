#nullable enable
using System.Text.Json.Serialization;

namespace SolTechnology.Core.CQRS
{
    public record Result
    {
        public bool IsSuccess { get; init; }

        [JsonIgnore]
        public bool IsFailure => !IsSuccess;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Error? Error { get; init; }


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
                Error = new Error
                {
                    Message = message
                },
                IsSuccess = false
            };
        }

        public static Result Fail(Error error)
        {
            return new Result
            {
                Error = error,
                IsSuccess = false
            };
        }

        public static Task<Result> FailAsTask(string message)
        {
            return Task.FromResult(new Result
            {
                Error = new Error
                {
                    Message = message
                },
                IsSuccess = false
            });
        }

        public static Task<Result> FailAsTask(Error error)
        {
            return Task.FromResult(new Result
            {
                Error = error,
                IsSuccess = false
            });
        }
    }

    public record Result<T> : Result
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public T Data { get; set; }

        public static implicit operator Result<T>(T value)
        {
            return Success(value);
        }

        public static implicit operator Result<T>(Exception e)
        {
            return Fail(e.Message);
        }

        public static implicit operator Result<T>(Error e)
        {
            return Fail(e);
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
                Error = new Error
                {
                    Message = message
                },
                IsSuccess = false
            };
        }

        public new static Result<T> Fail(Error error)
        {
            return new Result<T>
            {
                Error = error,
                IsSuccess = false
            };
        }


        public new static Task<Result<T>> FailAsTask(string message)
        {
            return Task.FromResult(new Result<T>
            {
                Error = new Error
                {
                    Message = message
                },
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

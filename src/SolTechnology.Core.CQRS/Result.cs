#nullable enable
using System.Text.Json.Serialization;
using SolTechnology.Core.CQRS.Errors;

namespace SolTechnology.Core.CQRS
{
    /// <summary>
    /// Represents the outcome of a command or query — either success or failure with an error.
    /// </summary>
    public record Result
    {
        public bool IsSuccess { get; init; }

        [JsonIgnore]
        public bool IsFailure => !IsSuccess;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Error? Error { get; init; }

        /// <summary>
        /// Non-generic accessor for the payload carried by <see cref="Result{T}"/>.
        /// Returns null on the base type.
        /// </summary>
        public virtual object? GetData() => null;

        public static Result Success() => new() { IsSuccess = true };

        public static Task<Result> SuccessAsTask() => Task.FromResult(Success());

        public static Result Fail(string message) => new()
        {
            IsSuccess = false,
            Error = new Error { Message = message }
        };

        public static Result Fail(Error error) => new()
        {
            IsSuccess = false,
            Error = error
        };

        public static Task<Result> FailAsTask(string message) => Task.FromResult(Fail(message));

        public static Task<Result> FailAsTask(Error error) => Task.FromResult(Fail(error));
    }

    /// <summary>
    /// Represents the outcome of a query or command that produces data of type <typeparamref name="T"/>.
    /// </summary>
    public record Result<T> : Result
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public T? Data { get; init; }

        public override object? GetData() => Data;

        public static implicit operator Result<T>(T value) => Success(value);

        public static implicit operator Result<T>(Error e) => Fail(e);

        public static Result<T> Success(T data) => new() { IsSuccess = true, Data = data };

        public new static Result<T> Fail(string message) => new()
        {
            IsSuccess = false,
            Error = new Error { Message = message }
        };

        public new static Result<T> Fail(Error error) => new()
        {
            IsSuccess = false,
            Error = error
        };

        public new static Task<Result<T>> FailAsTask(string message) => Task.FromResult(Fail(message));

        public static Task<Result<T>> SuccessAsTask(T data) => Task.FromResult(Success(data));
    }
}

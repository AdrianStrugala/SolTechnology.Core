#nullable enable
using System.Text.Json.Serialization;

namespace SolTechnology.Core;

/// <summary>
/// Represents the outcome of a command or query — either success or failure with an error.
/// Build instances through the static factories (<see cref="Success()"/>, <see cref="Fail(Error)"/>);
/// state cannot be set from outside, so a result is never both successful and failed.
/// </summary>
public record Result
{
    [JsonInclude]
    public bool IsSuccess { get; private protected init; }

    [JsonIgnore]
    public bool IsFailure => !IsSuccess;

    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Error? Error { get; private protected init; }

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
    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public T? Data { get; private init; }

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

    public new static Task<Result<T>> FailAsTask(Error error) => Task.FromResult(Fail(error));

    public static Task<Result<T>> SuccessAsTask(T data) => Task.FromResult(Success(data));
}


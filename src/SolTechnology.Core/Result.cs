#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SolTechnology.Core;

/// <summary>
/// Represents the outcome of a command or query — either success or failure with an error.
/// Build instances through the static factories (<see cref="Success()"/>, <see cref="Fail(Error)"/>);
/// state cannot be set from outside, so a result is never both successful and failed.
/// Deserialization rejects JSON whose <see cref="IsSuccess"/> and <see cref="Error"/> disagree.
/// </summary>
public record Result : IJsonOnDeserialized
{
    /// <summary>Reserved for the factories, derived types, and the JSON serializer.</summary>
    [JsonConstructor]
    protected Result() { }

    /// <summary>True when the operation succeeded.</summary>
    [JsonInclude]
    public bool IsSuccess { get; private protected init; }

    /// <summary>True when the operation failed; <see cref="Error"/> then describes why.</summary>
    [JsonIgnore]
    public bool IsFailure => !IsSuccess;

    /// <summary>The failure reason, or <c>null</c> when the operation succeeded.</summary>
    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Error? Error { get; private protected init; }

    /// <summary>
    /// Non-generic accessor for the payload carried by <see cref="Result{T}"/>.
    /// Returns null on the base type.
    /// </summary>
    public virtual object? GetData() => null;

    void IJsonOnDeserialized.OnDeserialized()
    {
        if (IsSuccess == (Error != null))
        {
            throw new JsonException(IsSuccess
                ? "A successful Result must not carry an Error."
                : "A failed Result must carry an Error.");
        }
    }

    /// <summary>Creates a successful result.</summary>
    public static Result Success() => new() { IsSuccess = true };

    /// <summary>Creates a successful result wrapped in a completed task.</summary>
    public static Task<Result> SuccessAsTask() => Task.FromResult(Success());

    /// <summary>Creates a failed result with a plain <see cref="Core.Error"/> carrying <paramref name="message"/>.</summary>
    public static Result Fail(string message) => new()
    {
        IsSuccess = false,
        Error = new Error { Message = message }
    };

    /// <summary>Creates a failed result with the given error.</summary>
    public static Result Fail(Error error) => new()
    {
        IsSuccess = false,
        Error = error
    };

    /// <summary>Creates a failed result with <paramref name="message"/>, wrapped in a completed task.</summary>
    public static Task<Result> FailAsTask(string message) => Task.FromResult(Fail(message));

    /// <summary>Creates a failed result with the given error, wrapped in a completed task.</summary>
    public static Task<Result> FailAsTask(Error error) => Task.FromResult(Fail(error));
}

/// <summary>
/// Represents the outcome of a query or command that produces data of type <typeparamref name="T"/>.
/// </summary>
public record Result<T> : Result
{
    /// <summary>Reserved for the factories, derived types, and the JSON serializer.</summary>
    [JsonConstructor]
    protected Result() { }

    /// <summary>The produced value on success; <c>default</c> on failure.</summary>
    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public T? Data { get; private init; }

    /// <summary>Returns <see cref="Data"/> as an object.</summary>
    public override object? GetData() => Data;

    /// <summary>Converts a value into a successful result.</summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>Converts an error into a failed result.</summary>
    public static implicit operator Result<T>(Error e) => Fail(e);

    /// <summary>Creates a successful result carrying <paramref name="data"/>.</summary>
    public static Result<T> Success(T data) => new() { IsSuccess = true, Data = data };

    /// <summary>Creates a failed result with a plain <see cref="Core.Error"/> carrying <paramref name="message"/>.</summary>
    public new static Result<T> Fail(string message) => new()
    {
        IsSuccess = false,
        Error = new Error { Message = message }
    };

    /// <summary>Creates a failed result with the given error.</summary>
    public new static Result<T> Fail(Error error) => new()
    {
        IsSuccess = false,
        Error = error
    };

    /// <summary>Creates a failed result with <paramref name="message"/>, wrapped in a completed task.</summary>
    public new static Task<Result<T>> FailAsTask(string message) => Task.FromResult(Fail(message));

    /// <summary>Creates a failed result with the given error, wrapped in a completed task.</summary>
    public new static Task<Result<T>> FailAsTask(Error error) => Task.FromResult(Fail(error));

    /// <summary>Creates a successful result carrying <paramref name="data"/>, wrapped in a completed task.</summary>
    public static Task<Result<T>> SuccessAsTask(T data) => Task.FromResult(Success(data));
}


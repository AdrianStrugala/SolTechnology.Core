using System.Text.Json.Serialization;

namespace SolTechnology.Core.Errors;

/// <summary>
/// Aggregates multiple errors into one. The <see cref="Message"/> property is computed
/// from all inner errors.
/// </summary>
public record AggregateError : Error
{
    private readonly IReadOnlyList<Error> _innerErrors;

    /// <summary>Creates an aggregate with no inner errors.</summary>
    public AggregateError()
    {
        _innerErrors = Array.Empty<Error>();
        Message = "One or more errors occurred.";
    }

    /// <summary>Creates an aggregate of <paramref name="innerErrors"/> with a default message.</summary>
    public AggregateError(IEnumerable<Error> innerErrors)
        : this("One or more errors occurred.", innerErrors)
    {
    }

    /// <summary>Creates an aggregate of <paramref name="innerErrors"/>; the message is <paramref name="message"/> followed by each inner message.</summary>
    public AggregateError(string message, IEnumerable<Error> innerErrors)
    {
        _innerErrors = (innerErrors ?? throw new ArgumentNullException(nameof(innerErrors))).ToArray();
        Message = BuildMessage(message, _innerErrors);
    }

    // Restores a serialized instance: Message was already built when the error was created.
    [JsonConstructor]
    private AggregateError(string message, IReadOnlyList<Error>? innerErrors)
    {
        _innerErrors = innerErrors ?? Array.Empty<Error>();
        Message = message;
    }

    /// <summary>The aggregated errors.</summary>
    public IReadOnlyList<Error> InnerErrors => _innerErrors;

    /// <summary>The base message followed by each inner error message in parentheses.</summary>
    public override string Message { get; init; }

    private static string BuildMessage(string baseMessage, IReadOnlyList<Error> errors)
    {
        if (errors.Count == 0)
        {
            return baseMessage;
        }

        return baseMessage + " " + string.Join(" ", errors.Select(e => $"({e.Message})"));
    }
}


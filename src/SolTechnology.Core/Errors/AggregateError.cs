using System.Text.Json.Serialization;

namespace SolTechnology.Core.Errors;

/// <summary>
/// Aggregates multiple errors into one. The <see cref="Message"/> property is computed
/// from all inner errors.
/// </summary>
public record AggregateError : Error
{
    private readonly IReadOnlyList<Error> _innerErrors;

    public AggregateError()
    {
        _innerErrors = Array.Empty<Error>();
        Message = "One or more errors occurred.";
    }

    public AggregateError(IEnumerable<Error> innerErrors)
        : this("One or more errors occurred.", innerErrors)
    {
    }

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

    public IReadOnlyList<Error> InnerErrors => _innerErrors;

    public override string Message { get; init; }

    private static string BuildMessage(string baseMessage, IReadOnlyList<Error> errors)
    {
        if (errors.Count == 0) return baseMessage;
        return baseMessage + " " + string.Join(" ", errors.Select(e => $"({e.Message})"));
    }
}


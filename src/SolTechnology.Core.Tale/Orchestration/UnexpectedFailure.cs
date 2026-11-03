using System.Diagnostics;

namespace SolTechnology.Core.Tale.Orchestration;

/// <summary>
/// Error stored and returned when a chapter or a tale throws. The exception is only logged: its text
/// can carry secrets, so it never reaches a result, an HTTP response or the persisted history.
/// </summary>
internal static class UnexpectedFailure
{
    public static Error Of(string message) => new()
    {
        Message = message,
        CorrelationId = Activity.Current?.TraceId.ToHexString()
    };
}

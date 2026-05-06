namespace SolTechnology.Core.Logging.Correlations;
/// <summary>
/// Ambient access point to the current <see cref="CorrelationId"/>. Allows code outside
/// of the ASP.NET Core request pipeline (background jobs, message-bus handlers,
/// outgoing <c>HttpClient</c> handlers) to read or propagate the correlation.
/// </summary>
public interface ICorrelationIdService
{
    /// <summary>Stores <paramref name="correlationId"/> for the current async-flow.</summary>
    void Set(CorrelationId correlationId);

    /// <summary>Returns the correlation set for the current async-flow, or <c>null</c> if none.</summary>
    CorrelationId? Get();

    /// <summary>
    /// Returns the current correlation, generating + setting a new one when none is present.
    /// Use from non-HTTP entry points (background jobs, scheduled tasks) to ensure correlation
    /// always exists in the scope.
    /// </summary>
    CorrelationId GetOrGenerate();
}

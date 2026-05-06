namespace SolTechnology.Core.Logging.Correlations;
/// <summary>
/// Ambient access point to the current <see cref="CorrelationId"/>. Allows code outside
/// of the ASP.NET Core request pipeline (background jobs, message-bus handlers,
/// outgoing <c>HttpClient</c> handlers) to read or propagate the correlation.
/// </summary>
public interface ICorrelationIdService
{
    void Set(CorrelationId correlationId);
    CorrelationId? Get();
    CorrelationId GetOrGenerate();
}

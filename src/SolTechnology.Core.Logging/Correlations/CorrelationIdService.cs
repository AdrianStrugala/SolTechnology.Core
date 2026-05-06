namespace SolTechnology.Core.Logging.Correlations;
/// <inheritdoc />
internal sealed class CorrelationIdService : ICorrelationIdService
{
    private static readonly AsyncLocal<CorrelationId?> Context = new();
    public void Set(CorrelationId correlationId) => Context.Value = correlationId;
    public CorrelationId? Get() => Context.Value;
    public CorrelationId GetOrGenerate()
    {
        var existing = Context.Value;
        if (existing is not null)
        {
            return existing;
        }
        var generated = CorrelationId.Generate();
        Context.Value = generated;
        return generated;
    }
}

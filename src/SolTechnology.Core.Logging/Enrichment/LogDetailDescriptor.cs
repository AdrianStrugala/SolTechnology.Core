namespace SolTechnology.Core.Logging.Enrichment;

/// <summary>
/// One <c>LogDetail(...)</c> registration. Multiple descriptors are allowed in DI;
/// they are aggregated by the built-in enricher.
/// </summary>
internal sealed record LogDetailDescriptor(
    string PropertyName,
    string ScopeName,
    LogDetailSource Source,
    IReadOnlyList<string>? Endpoints);



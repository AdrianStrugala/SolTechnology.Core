using System.Diagnostics;
using SolTechnology.Core.Logging.Operations;

namespace SolTechnology.Core.CQRS.PipelineBehaviors;

/// <summary>
/// The CQRS-owned <see cref="ActivitySource"/> on <see cref="CoreLoggingActivitySources.OperationsName"/>,
/// so a listener subscribed to that name observes every pipeline operation.
/// </summary>
internal static class OperationsActivitySource
{
    // Not a field on the generic behavior: that would create one ActivitySource per closed type.
    public static readonly ActivitySource Instance = new(CoreLoggingActivitySources.OperationsName);
}

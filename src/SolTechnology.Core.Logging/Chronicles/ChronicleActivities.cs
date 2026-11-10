using System.Diagnostics;
using SolTechnology.Core.Logging.Operations;

namespace SolTechnology.Core.Logging.Chronicles;

/// <summary>
/// Chronicle steps as <see cref="Activity"/> spans on <see cref="CoreLoggingActivitySources.OperationsName"/>.
/// Without a listener a start costs one <see cref="ActivitySource.HasListeners"/> check.
/// </summary>
internal static class ChronicleActivities
{
    internal const string StatusTag = "chronicle.status";

    internal static Activity? Start(ChronicleOptions options, string name)
        => options.EmitActivities && CoreLoggingActivitySources.Operations.HasListeners()
            ? CoreLoggingActivitySources.Operations.StartActivity(name, ActivityKind.Internal)
            : null;

    internal static string? CurrentTraceId()
        => Activity.Current is { IdFormat: ActivityIdFormat.W3C } activity ? activity.TraceId.ToHexString() : null;

    internal static void Fail(Activity activity, string errorType, string? message, Exception? exception)
    {
        activity.SetStatus(ActivityStatusCode.Error, message ?? errorType);
        activity.SetTag("error.type", errorType);
        if (exception is not null)
        {
            activity.AddException(exception);
        }
    }

    internal static void Cancel(Activity activity) => activity.SetTag(StatusTag, "cancelled");
}

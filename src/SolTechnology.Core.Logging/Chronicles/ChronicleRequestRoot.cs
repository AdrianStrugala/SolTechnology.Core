using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.Logging.Chronicles;

/// <summary>
/// The Chronicle root of one HTTP request: opened by <c>LoggingMiddleware</c> without an activity
/// (ASP.NET Core owns the request activity) and reported by the middleware itself as the request's
/// finish entry (EventId 2155), so the reporter never writes a second entry for it.
/// </summary>
internal static class ChronicleRequestRoot
{
    internal const string UnmatchedRoute = "unmatched";

    internal static ChronicleStep Begin(IChronicle chronicle, HttpContext context)
    {
        var name = "HTTP " + context.Request.Method;
        return chronicle is ChronicleRecorder recorder
            ? recorder.BeginWithoutActivity(name, reportedByCaller: true)
            : chronicle.Begin(name);
    }

    /// <summary>
    /// Records the outcome and closes the root. Returns the closed root when the caller must report it;
    /// <c>null</c> when nothing was recorded or another owner reports it (the caller logs its fallback entry).
    /// </summary>
    internal static ChronicleRoot? Complete(ChronicleStep root, HttpContext context, Exception? exception, bool clientAborted)
    {
        var statusCode = context.Response.StatusCode;
        if (root.IsRecording)
        {
            root.Set("Route", RouteOf(context)).Set("StatusCode", statusCode);
        }

        if (clientAborted)
        {
            root.Cancel();
        }
        else if (exception is not null)
        {
            root.Fail(exception);
        }
        else if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            // A 5xx without an exception: an MVC filter mapped the failure (CRM-5).
            root.Fail("HTTP" + statusCode);
        }

        root.Dispose();
        return root.Root is { IsReportedByCaller: true, IsClosed: true } closed ? closed : null;
    }

    /// <summary>Writes the 2155 entry at the level the emission policy picks; sampling may drop a fast success.</summary>
    internal static void Report(ILogger logger, ChronicleRoot root, HttpContext context, Exception? exception, long elapsedMs)
    {
        try
        {
            var statusCode = context.Response.StatusCode;
            var policy = root.Owner.Policy;
            var level = policy.LevelOf(root.Node.Status, root.Duration, statusCode);
            if (!logger.IsEnabled(level))
            {
                return;
            }

            var report = ChronicleReportBuilder.Build(root);
            if (policy.ShouldEmit(report, level))
            {
                ChronicleLogMessages.RequestChronicleReported(logger, level, exception, context.Request.Method,
                    context.Request.Path, statusCode, elapsedMs, report.Status.ToString(), report);
            }
        }
        catch (Exception reportFailure)
        {
            logger.LogWarning(reportFailure, "Chronicle could not report [{ChronicleName}]", root.Node.Name);
        }
    }

    private static string RouteOf(HttpContext context)
        => context.GetEndpoint() is RouteEndpoint { RoutePattern.RawText: { } template } ? template : UnmatchedRoute;
}

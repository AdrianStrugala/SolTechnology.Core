namespace SolTechnology.Core.API.Exceptions;

/// <summary>
/// Maps exception types to HTTP status codes for the API exception filter.
/// <para>
/// Replace the default registration to extend or customize the map:
/// </para>
/// <code>
/// public sealed class AppExceptionMapper : DefaultExceptionStatusCodeMapper
/// {
///     public override bool TryMap(Exception exception, out int statusCode)
///     {
///         if (exception is MyDomainTimeoutException) { statusCode = 504; return true; }
///
///         // Per-upstream granularity: surface a specific dependency as 503 instead of
///         // the default 502, so dashboards can distinguish "Stripe is down" from
///         // "any upstream is down".
///         if (exception is HttpRequestException http &amp;&amp;
///             http.Data.Contains("Upstream") &amp;&amp; (string?)http.Data["Upstream"] == "Stripe")
///         {
///             statusCode = 503;
///             return true;
///         }
///
///         return base.TryMap(exception, out statusCode);
///     }
/// }
///
/// services.AddApiExceptionHandling();
/// services.Replace(ServiceDescriptor.Singleton&lt;IExceptionStatusCodeMapper, AppExceptionMapper&gt;());
/// </code>
/// <para>
/// Returning <c>false</c> triggers the A+E policy in the filter: <c>LogCritical</c> + rethrow
/// to the host pipeline. This is intentional — every <c>false</c> in production logs marks an
/// exception type the team has not yet decided how to surface, and is a signal to extend the
/// map rather than silently default to 500.
/// </para>
/// </summary>
public interface IExceptionStatusCodeMapper
{
    /// <summary>
    /// Returns <c>true</c> when the exception is recognized; <c>statusCode</c> is the HTTP
    /// status the API should respond with. Returns <c>false</c> for unknown types — the filter
    /// will then rethrow to the host.
    /// </summary>
    bool TryMap(Exception exception, out int statusCode);
}


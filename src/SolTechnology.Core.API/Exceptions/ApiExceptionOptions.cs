namespace SolTechnology.Core.API.Exceptions;

/// <summary>
/// Options governing how unhandled exceptions are turned into HTTP response bodies by
/// <see cref="Filters.ExceptionFilter"/>.
/// Configure via <c>services.AddApiExceptionHandling(opts =&gt; ...)</c>.
/// </summary>
public class ApiExceptionOptions
{
    /// <summary>
    /// <para>
    /// When <c>true</c>, the response body's <c>Error.Description</c> is augmented with the
    /// exception's <em>type</em> and <em>stack trace</em>. Useful for local debugging and
    /// integration tests.
    /// </para>
    /// <para>
    /// Defaults to <c>false</c>. Leaking stack traces over the wire in Production is
    /// information disclosure (CWE-209) — leave this off unless you fully control who
    /// reaches the endpoint. The convention is to enable it explicitly in
    /// <c>Development</c>:
    /// </para>
    /// <code>
    /// services.AddApiExceptionHandling(o =&gt;
    ///     o.IncludeExceptionDetails = builder.Environment.IsDevelopment());
    /// </code>
    /// </summary>
    public bool IncludeExceptionDetails { get; set; }
}


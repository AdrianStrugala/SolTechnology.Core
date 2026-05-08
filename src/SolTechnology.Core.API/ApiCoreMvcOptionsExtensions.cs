using Microsoft.AspNetCore.Mvc;
using SolTechnology.Core.API.Filters;

namespace SolTechnology.Core.API;

/// <summary>
/// MVC pipeline extensions provided by <c>SolTechnology.Core.Api</c>.
/// </summary>
public static class ApiCoreMvcOptionsExtensions
{
    /// <summary>
    /// Registers the two filters required by the API core pipeline globally:
    /// <list type="bullet">
    ///   <item><see cref="ExceptionFilter"/> — converts mapped exceptions to RFC 7807
    ///         <c>ProblemDetails</c>; rethrows unmapped ones to the host.</item>
    ///   <item><see cref="ResultConversionFilter"/> — unwraps <c>Result&lt;T&gt;</c> /
    ///         <c>Result</c> from controller actions to the wire format (raw DTO on success,
    ///         <c>ProblemDetails</c> on failure).</item>
    /// </list>
    /// <para>
    /// Both filter types must be registered in DI first. The recommended way is
    /// <c>services.AddApiCore(...)</c> on <see cref="ModuleInstaller"/>, which performs the
    /// registration and configures all dependencies (correlation id, problem details, exception
    /// status mapper, options).
    /// </para>
    /// <code>
    /// builder.Services.AddApiCore(o =&gt;
    ///     o.IncludeExceptionDetails = builder.Environment.IsDevelopment());
    ///
    /// builder.Services.AddControllers(opts =&gt;
    /// {
    ///     opts.AddApiCoreFilters();
    ///     // ... project-specific filters
    /// });
    /// </code>
    /// </summary>
    public static MvcOptions AddApiCoreFilters(this MvcOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Filters.Add<ExceptionFilter>();
        options.Filters.Add<ResultConversionFilter>();
        return options;
    }
}



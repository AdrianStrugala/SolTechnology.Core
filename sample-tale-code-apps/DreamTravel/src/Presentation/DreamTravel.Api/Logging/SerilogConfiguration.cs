using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace DreamTravel.Api.Logging;

/// <summary>
/// Extension methods for configuring Serilog with sensible defaults.
/// </summary>
public static class SerilogConfiguration
{
    /// <summary>
    /// Configures Serilog with sensible defaults for ASP.NET Core applications.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="identifiers">Optional list of identifier names to include in log output template.</param>
    /// <returns>The web application builder for chaining.</returns>
    /// <example>
    /// <code>
    /// var builder = WebApplication.CreateBuilder(args);
    /// builder.UseSerilogDefaults(["trip", "city"]);
    /// </code>
    /// </example>
    public static WebApplicationBuilder UseSerilogDefaults(
        this WebApplicationBuilder builder,
        string[]? identifiers = null)
    {
        var outputTemplate = BuildOutputTemplate(identifiers);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .WriteTo.Console(outputTemplate: outputTemplate)
            .CreateLogger();

        builder.Host.UseSerilog();

        return builder;
    }

    /// <summary>
    /// Configures Serilog with sensible defaults for generic host applications.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="identifiers">Optional list of identifier names to include in log output template.</param>
    /// <returns>The host builder for chaining.</returns>
    public static IHostBuilder UseSerilogDefaults(
        this IHostBuilder builder,
        string[]? identifiers = null)
    {
        var outputTemplate = BuildOutputTemplate(identifiers);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .WriteTo.Console(outputTemplate: outputTemplate)
            .CreateLogger();

        return builder.UseSerilog();
    }

    private static string BuildOutputTemplate(string[]? identifiers)
    {
        var identifiersPart = identifiers?.Length > 0
            ? " " + string.Join(" ", identifiers.Select(id => $"{{{id}}}"))
            : "";

        return $"[{{Timestamp:HH:mm:ss}} {{Level:u3}}] {{CorrelationId}}{identifiersPart} {{Message:lj}}{{NewLine}}{{Exception}}";
    }
}

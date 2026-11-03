using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SolTechnology.Core.Logging.Correlations;
using SolTechnology.Core.Logging.Enrichment;

namespace SolTechnology.Core.Logging;

/// <summary>
/// The registrations behind <see cref="ModuleInstaller"/>: one path shared by both
/// <c>AddSolLogging</c> overloads, plus the idempotent built-in enricher registrations.
/// </summary>
internal static class LoggingServiceRegistrations
{
    public static OptionsBuilder<LoggingOptions> RegisterCore(IServiceCollection services)
    {
        services.TryAddSingleton<ICorrelationIdService, CorrelationIdService>();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<ITimingService, TimingService>();
        EnsureRequestHeadersEnricherRegistered(services);

        return services.AddOptions<LoggingOptions>()
            .ValidateDataAnnotations()
            .ValidateOnStart()
            .Validate(o => o.SkipPaths is null || o.SkipPaths.All(p => !string.IsNullOrWhiteSpace(p)),
                "LoggingOptions.SkipPaths must not contain null or whitespace entries.")
            .Validate(o => o.MaskedHeaders is null || o.MaskedHeaders.All(p => !string.IsNullOrWhiteSpace(p)),
                "LoggingOptions.MaskedHeaders must not contain null or whitespace entries.");
    }

    public static void EnsureLogDetailEnricherRegistered(IServiceCollection services)
    {
        if (services.Any(d => d.ImplementationType == typeof(LogDetailEnricher)))
        {
            return;
        }

        services.AddSingleton<LogDetailEnricher>();
        services.AddSingleton<ILogScopeEnricher>(sp => sp.GetRequiredService<LogDetailEnricher>());
    }

    private static void EnsureRequestHeadersEnricherRegistered(IServiceCollection services)
    {
        if (services.Any(d => d.ImplementationType == typeof(RequestHeadersEnricher)))
        {
            return;
        }

        services.AddSingleton<RequestHeadersEnricher>();
        services.AddSingleton<ILogScopeEnricher>(sp => sp.GetRequiredService<RequestHeadersEnricher>());
    }
}

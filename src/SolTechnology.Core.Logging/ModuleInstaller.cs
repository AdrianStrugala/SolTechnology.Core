using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SolTechnology.Core.Logging.Correlations;
using SolTechnology.Core.Logging.Enrichment;
using SolTechnology.Core.Logging.Middleware;

namespace SolTechnology.Core.Logging;

public static class LoggingServiceCollectionExtensions
{
    /// <param name="services">DI container.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the correlation service used by <see cref="LoggingMiddleware"/>.
        /// Safe to call multiple times.
        /// </summary>
        public IServiceCollection AddCoreLogging(Action<LoggingOptions>? configure = null)
        {
            services.TryAddSingleton<ICorrelationIdService, CorrelationIdService>();

            var options = services.AddOptions<LoggingOptions>();
            if (configure is not null)
            {
                options.Configure(configure);
            }

            return services;
        }

        /// <summary>
        /// Registers a property to be added to every per-request log scope, with the parsing logic
        /// owned by the library. Multiple <c>LogDetail</c> calls compose; one descriptor per call.
        /// </summary>
        /// <param name="propertyName">
        /// Name of the source property: header name (<see cref="LogDetailSource.Header"/>),
        /// query/route key (<see cref="LogDetailSource.Url"/>), or JSON body field name
        /// (<see cref="LogDetailSource.Body"/>; case-insensitive PascalCase/camelCase fallback).
        /// </param>
        /// <param name="asName">
        /// Scope-property name. Defaults to <paramref name="propertyName"/>. Use this to project
        /// e.g. body field <c>name</c> as <c>CityName</c> in the logs.
        /// </param>
        /// <param name="source">Where to read the value from. Defaults to <see cref="LogDetailSource.Body"/>.</param>
        /// <param name="endpoints">
        /// Optional list of request-path prefixes (case-insensitive) the rule applies to.
        /// <c>null</c> or empty = all endpoints. Path-prefix match: e.g. <c>/api/v1/Trips</c>
        /// matches <c>/api/v1/Trips/42</c>.
        /// </param>
        public IServiceCollection LogDetail(string propertyName,
            string? asName = null,
            LogDetailSource source = LogDetailSource.Body,
            IReadOnlyCollection<string>? endpoints = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

            var descriptor = new LogDetailDescriptor(
                PropertyName: propertyName,
                ScopeName: string.IsNullOrWhiteSpace(asName) ? propertyName : asName,
                Source: source,
                Endpoints: endpoints is { Count: > 0 } ? endpoints.ToArray() : null);

            services.AddSingleton(descriptor);
            EnsureLogDetailEnricherRegistered(services);
            return services;
        }

        /// <summary>
        /// Registers a custom <see cref="ILogScopeEnricher"/> when <see cref="LogDetail"/> is not
        /// expressive enough (e.g. composing a value from claims, multiple sources, async lookups).
        /// </summary>
        public IServiceCollection AddLogScopeEnricher<TEnricher>()
            where TEnricher : class, ILogScopeEnricher
        {
            services.AddSingleton<ILogScopeEnricher, TEnricher>();
            return services;
        }
    }

    /// <summary>
    /// Adds the request-logging middleware. Should be registered early in the pipeline
    /// (before <c>UseRouting</c>) so that all requests are observed.
    /// </summary>
    public static IApplicationBuilder UseCoreLogging(this IApplicationBuilder app)
        => app.UseMiddleware<LoggingMiddleware>();

    private static void EnsureLogDetailEnricherRegistered(IServiceCollection services)
    {
        if (services.Any(d => d.ImplementationType == typeof(LogDetailEnricher)))
        {
            return;
        }

        services.AddSingleton<LogDetailEnricher>();
        services.AddSingleton<ILogScopeEnricher>(sp => sp.GetRequiredService<LogDetailEnricher>());
    }
}


using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using SolTechnology.Core.Logging.Correlations;
using SolTechnology.Core.Logging.Enrichment;
using SolTechnology.Core.Logging.Middleware;

namespace SolTechnology.Core.Logging;

/// <summary>
/// DI / pipeline wiring for <c>SolTechnology.Core.Logging</c>. Use
/// <c>AddCoreLogging</c> in <c>ConfigureServices</c> and <see cref="UseCoreLogging"/>
/// in the request pipeline (early — before <c>UseRouting</c>).
/// </summary>
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

            var optionsBuilder = services.AddOptions<LoggingOptions>()
                .ValidateDataAnnotations()
                .ValidateOnStart()
                .Validate(o => o.SkipPaths is null || o.SkipPaths.All(p => !string.IsNullOrWhiteSpace(p)),
                    "LoggingOptions.SkipPaths must not contain null or whitespace entries.")
                .Validate(o => o.MaskedHeaders is null || o.MaskedHeaders.All(p => !string.IsNullOrWhiteSpace(p)),
                    "LoggingOptions.MaskedHeaders must not contain null or whitespace entries.");

            if (configure is not null)
            {
                optionsBuilder.Configure(configure);
            }

            EnsureRequestHeadersEnricherRegistered(services);
            return services;
        }

        /// <summary>
        /// Binds <see cref="LoggingOptions"/> from the configuration section
        /// <c>Logging:Core</c> (see <see cref="LoggingOptions.SectionName"/>) and registers
        /// the correlation service.
        /// </summary>
        public IServiceCollection AddCoreLogging(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            services.TryAddSingleton<ICorrelationIdService, CorrelationIdService>();

            services.AddOptions<LoggingOptions>()
                .Bind(configuration.GetSection(LoggingOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart()
                .Validate(o => o.SkipPaths is null || o.SkipPaths.All(p => !string.IsNullOrWhiteSpace(p)),
                    "LoggingOptions.SkipPaths must not contain null or whitespace entries.")
                .Validate(o => o.MaskedHeaders is null || o.MaskedHeaders.All(p => !string.IsNullOrWhiteSpace(p)),
                    "LoggingOptions.MaskedHeaders must not contain null or whitespace entries.");

            EnsureRequestHeadersEnricherRegistered(services);
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

        /// <summary>
        /// Registers only the <see cref="ICorrelationIdService"/> — without the request-
        /// logging middleware or <see cref="LoggingOptions"/> apparatus that
        /// <c>AddCoreLogging</c> brings.
        /// <para>
        /// Intended for non-ASP.NET consumers (workers, background jobs, library packages
        /// such as <c>SolTechnology.Core.HTTP</c>) that need to read or seed the ambient
        /// correlation id but don't host an HTTP pipeline. Idempotent — safe to call from
        /// multiple installers; <c>AddCoreLogging</c> reuses the same registration.
        /// </para>
        /// </summary>
        public IServiceCollection AddCorrelationIdService()
        {
            services.TryAddSingleton<ICorrelationIdService, CorrelationIdService>();
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


using Hangfire;
using Hangfire.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Hangfire.Filters;
using SolTechnology.Core.Logging;

namespace SolTechnology.Core.Hangfire;

/// <summary>
/// Registration entrypoint for the Hangfire plugin.
/// </summary>
public static class ModuleInstaller
{
    /// <summary>
    /// Replaces the in-memory event publisher with a Hangfire-backed durable publisher.
    /// Requires <c>AddSolCQRS()</c> first. The app must also call <c>AddHangfire(...)</c> and
    /// <c>AddHangfireServer()</c> with a DI-aware activator and type-aware serializer settings.
    /// </summary>
    public static IServiceCollection AddSolPersistentEvents(
        this IServiceCollection services,
        Action<PersistentEventsOptions>? configure = null)
    {
        if (!services.Any(d => d.ServiceType == typeof(IEventDispatcher)))
        {
            throw new InvalidOperationException(
                "AddSolPersistentEvents() requires AddSolCQRS() to be called first.");
        }

        var options = new PersistentEventsOptions();
        configure?.Invoke(options);
        services.Configure<PersistentEventsOptions>(o => o.QueueName = options.QueueName);

        services.RemoveAll<IEventPublisher>();
        services.AddSingleton<IEventPublisher, HangfireEventPublisher>();

        // Filters
        services.AddSolCorrelationIdService();
        services.TryAddSingleton<CorrelationIdJobFilter>();
        services.TryAddSingleton<SmartRetryJobFilter>();

        return services;
    }

    /// <summary>
    /// Registers a recurring job that runs on the given cron schedule via Hangfire.
    /// The app must call <c>AddHangfire(...)</c> and <c>AddHangfireServer()</c>.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="cronExpression">Cron expression (use <see cref="Cron"/> helpers).</param>
    /// <param name="preventOverlap">
    /// When true, a run is cancelled while another run of the same job is processing.
    /// Requires <see cref="UseSolFilters"/>.
    /// </param>
    /// <param name="jobId">Recurring job id; defaults to the version-free full type name of <typeparamref name="TJob"/>.</param>
    /// <param name="timeZone">Time zone of <paramref name="cronExpression"/>; defaults to UTC.</param>
    public static IServiceCollection AddSolRecurringJob<TJob>(
        this IServiceCollection services,
        string cronExpression,
        bool preventOverlap = false,
        string? jobId = null,
        TimeZoneInfo? timeZone = null) where TJob : class, IJob
    {
        ArgumentNullException.ThrowIfNull(cronExpression);
        if (jobId is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(jobId);
        }

        services.AddScoped<TJob>();
        services.AddSingleton(new RecurringJobDescriptor(typeof(TJob), cronExpression, preventOverlap, jobId, timeZone));
        services.AddSingleton<RecurringJobRunner<TJob>>();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IHostedService, RecurringJobRegistrar>());
        services.TryAddSingleton<PreventOverlapFilterProvider>();

        // Filters (shared with persistent events)
        services.AddSolCorrelationIdService();
        services.TryAddSingleton<CorrelationIdJobFilter>();
        services.TryAddSingleton<SmartRetryJobFilter>();

        return services;
    }

    /// <summary>
    /// Adds the Hangfire global job filters (correlation-id propagation, smart retry) and the
    /// overlap guard for recurring jobs registered with <c>preventOverlap: true</c>.
    /// Call from the app's <c>AddHangfire</c> configuration callback:
    /// <code>
    /// services.AddHangfire((sp, config) => config.UseSolFilters(sp));
    /// </code>
    /// </summary>
    public static IGlobalConfiguration UseSolFilters(
        this IGlobalConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        var correlationFilter = serviceProvider.GetService<CorrelationIdJobFilter>();
        if (correlationFilter is not null)
        {
            configuration.UseFilter(correlationFilter);
        }

        var smartRetryFilter = serviceProvider.GetService<SmartRetryJobFilter>();
        if (smartRetryFilter is not null)
        {
            configuration.UseFilter(smartRetryFilter);
        }

        serviceProvider.GetService<PreventOverlapFilterProvider>()?.InstallInto(JobFilterProviders.Providers);

        return configuration;
    }
}



using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SolTechnology.Core.CQRS;

namespace SolTechnology.Core.Hangfire;

/// <summary>
/// Registration entrypoint for the Hangfire plugin.
/// </summary>
public static class ModuleInstaller
{
    /// <summary>
    /// Replaces the in-memory event publisher with a Hangfire-backed durable publisher.
    /// Requires <c>AddCQRS()</c> first. The app must also call <c>AddHangfire(...)</c> and
    /// <c>AddHangfireServer()</c> with a DI-aware activator and type-aware serializer settings.
    /// </summary>
    public static IServiceCollection AddPersistentEvents(
        this IServiceCollection services,
        Action<PersistentEventsOptions>? configure = null)
    {
        if (!services.Any(d => d.ServiceType == typeof(IEventDispatcher)))
        {
            throw new InvalidOperationException(
                "AddPersistentEvents() requires AddCQRS() to be called first.");
        }

        var options = new PersistentEventsOptions();
        configure?.Invoke(options);
        services.Configure<PersistentEventsOptions>(o => o.QueueName = options.QueueName);

        services.RemoveAll<IEventPublisher>();
        services.AddSingleton<IEventPublisher, HangfireEventPublisher>();

        return services;
    }

    /// <summary>
    /// Registers a recurring job that runs on the given cron schedule via Hangfire.
    /// The app must call <c>AddHangfire(...)</c> and <c>AddHangfireServer()</c>.
    /// </summary>
    public static IServiceCollection AddRecurringJob<TJob>(
        this IServiceCollection services,
        string cronExpression) where TJob : class, IJob
    {
        ArgumentNullException.ThrowIfNull(cronExpression);

        services.AddScoped<TJob>();
        services.AddSingleton(new RecurringJobDescriptor(typeof(TJob), cronExpression));
        services.AddSingleton<RecurringJobRunner<TJob>>();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IHostedService, RecurringJobRegistrar>());

        return services;
    }
}



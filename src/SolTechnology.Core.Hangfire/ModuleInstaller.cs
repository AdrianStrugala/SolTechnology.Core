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
    /// <param name="services">Service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Replaces the in-memory event publisher with a Hangfire-backed durable publisher.
        /// Requires <c>AddSolCQRS()</c> first. The app must also call <c>AddHangfire(...)</c> and
        /// <c>AddHangfireServer()</c> with a DI-aware activator and type-aware serializer settings.
        /// </summary>
        public IServiceCollection AddSolPersistentEvents(Action<PersistentEventsOptions>? configure = null)
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
            services.AddSolJobFilters();

            return services;
        }

        /// <summary>
        /// Registers a recurring job that runs on the given cron schedule via Hangfire.
        /// The app must call <c>AddHangfire(...)</c> and <c>AddHangfireServer()</c>.
        /// </summary>
        /// <param name="cronExpression">Cron expression (use <see cref="Cron"/> helpers).</param>
        /// <param name="preventOverlap">
        /// When true, a run is cancelled while another run of the same job is processing.
        /// Requires <c>UseSolFilters</c>.
        /// </param>
        /// <param name="jobId">
        /// Recurring job id of at most 86 characters; defaults to the version-free full type name of
        /// <typeparamref name="TJob"/>, shortened to the type name plus a hash when longer than 86 characters.
        /// </param>
        /// <exception cref="ArgumentException"><paramref name="jobId"/> is whitespace or longer than 86 characters.</exception>
        /// <param name="timeZone">Time zone of <paramref name="cronExpression"/>; defaults to UTC.</param>
        public IServiceCollection AddSolRecurringJob<TJob>(
            string cronExpression,
            bool preventOverlap = false,
            string? jobId = null,
            TimeZoneInfo? timeZone = null) where TJob : class, IJob
        {
            ArgumentNullException.ThrowIfNull(cronExpression);
            if (jobId is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(jobId);
                if (jobId.Length > StorageKey.MaxRecurringJobIdLength)
                {
                    throw new ArgumentException(
                        $"Recurring job id must be at most {StorageKey.MaxRecurringJobIdLength} characters, because Hangfire.SqlServer stores 'recurring-job:<id>' in an nvarchar(100) key; got {jobId.Length}.",
                        nameof(jobId));
                }
            }

            services.AddScoped<TJob>();
            services.AddSingleton(new RecurringJobDescriptor(typeof(TJob), cronExpression, preventOverlap, jobId, timeZone));
            services.AddSingleton<RecurringJobRunner<TJob>>();

            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IHostedService, RecurringJobRegistrar>());
            services.TryAddSingleton<PreventOverlapFilterProvider>();
            services.TryAddSingleton<RecurringJobCleanup>();
            services.AddOptions<RecurringJobsOptions>();
            services.AddSolJobFilters();

            return services;
        }

        // TryAdd: persistent events and recurring jobs share one instance of each filter.
        private void AddSolJobFilters()
        {
            services.AddSolCorrelationIdService();
            services.TryAddSingleton<CorrelationIdJobFilter>();
            services.TryAddSingleton<SmartRetryJobFilter>();
            services.AddSolChronicle();
            services.TryAddSingleton<ChronicleJobFilter>();
        }
    }

    /// <param name="configuration">The Hangfire configuration of the app's <c>AddHangfire</c> callback.</param>
    extension(IGlobalConfiguration configuration)
    {
        /// <summary>
        /// Adds the Hangfire global job filters (correlation-id propagation, smart retry, a Chronicle root per
        /// performed job) and the overlap guard for recurring jobs registered with <c>preventOverlap: true</c>.
        /// Call from the app's <c>AddHangfire</c> configuration callback:
        /// <code>
        /// services.AddHangfire((sp, config) => config.UseSolFilters(sp));
        /// </code>
        /// </summary>
        public IGlobalConfiguration UseSolFilters(IServiceProvider serviceProvider)
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

            if (serviceProvider.GetService<ChronicleJobFilter>() is { } chronicleFilter)
            {
                configuration.UseFilter(chronicleFilter);
            }

            serviceProvider.GetService<PreventOverlapFilterProvider>()?.InstallInto(JobFilterProviders.Providers);

            return configuration;
        }
    }
}

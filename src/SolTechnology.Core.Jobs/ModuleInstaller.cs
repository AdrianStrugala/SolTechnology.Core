using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace SolTechnology.Core.Jobs;

/// <summary>
/// Extension methods for registering SolTechnology.Core.Jobs services.
/// </summary>
public static class ModuleInstaller
{
    /// <summary>
    /// Default retry delays: 10s, 1m, 5m, 30m, 1h, 2h, 4h, 8h, 16h, 24h
    /// </summary>
    public static readonly int[] DefaultRetryDelaysInSeconds = [10, 60, 300, 1800, 3600, 7200, 14400, 28800, 57600, 86400];

    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds Hangfire utilities: smart retry filter and correlation ID propagation.
        /// </summary>
        /// <param name="options">Optional configuration for job processing.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddSolHangfire(JobsOptions? options = null)
        {
            options ??= new JobsOptions();

            if (options.EnableSmartRetry)
            {
                GlobalJobFilters.Filters.Add(new HangfireSmartRetryAttribute(options.RetryDelaysInSeconds));
            }

            if (options.EnableCorrelationIdPropagation)
            {
                GlobalJobFilters.Filters.Add(new HangfireCorrelationIdFilter());
            }

            return services;
        }

        /// <summary>
        /// Adds Hangfire utilities with fluent configuration.
        /// </summary>
        /// <param name="configure">Action to configure job processing options.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddSolHangfire(Action<JobsOptions> configure)
        {
            var options = new JobsOptions();
            configure(options);
            return services.AddSolHangfire(options);
        }

        /// <summary>
        /// Adds only the smart retry filter with custom delays.
        /// </summary>
        /// <param name="delaysInSeconds">Optional custom retry delays in seconds. Uses default delays if not specified.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddHangfireSmartRetry(int[]? delaysInSeconds = null)
        {
            var delays = delaysInSeconds ?? DefaultRetryDelaysInSeconds;
            GlobalJobFilters.Filters.Add(new HangfireSmartRetryAttribute(delays));
            return services;
        }

        /// <summary>
        /// Adds only the correlation ID filter for tracking jobs across async boundaries.
        /// </summary>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddHangfireCorrelationId()
        {
            GlobalJobFilters.Filters.Add(new HangfireCorrelationIdFilter());
            return services;
        }

        /// <summary>
        /// Adds the Hangfire event publisher for MediatR notification dispatch.
        /// </summary>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddHangfireEventPublisher()
        {
            services.AddTransient<IHangfireNotificationPublisher, HangfireNotificationPublisher>();
            return services;
        }
    }
}

/// <summary>
/// Configuration options for Hangfire job processing.
/// </summary>
public class JobsOptions
{
    /// <summary>
    /// Gets or sets whether to enable the smart retry filter with exponential backoff.
    /// Default: true
    /// </summary>
    public bool EnableSmartRetry { get; set; } = true;

    /// <summary>
    /// Gets or sets the retry delays in seconds for the smart retry filter.
    /// Default: 10s, 1m, 5m, 30m, 1h, 2h, 4h, 8h, 16h, 24h
    /// </summary>
    public int[] RetryDelaysInSeconds { get; set; } = ModuleInstaller.DefaultRetryDelaysInSeconds;

    /// <summary>
    /// Gets or sets whether to enable correlation ID propagation through background jobs.
    /// Default: true
    /// </summary>
    public bool EnableCorrelationIdPropagation { get; set; } = true;
}

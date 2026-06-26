using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using SolTechnology.Core.MessageBus.Configuration;

namespace SolTechnology.Core.MessageBus.HealthChecks;

/// <summary>
/// Registration extension for the Service Bus connectivity health check on the framework
/// <see cref="IHealthChecksBuilder"/>.
/// </summary>
public static class ServiceBusHealthCheckExtensions
{
    /// <summary>
    /// Adds a Service Bus liveness check. Requires <see cref="ServiceBusClient"/> and
    /// <see cref="MessageBusConfiguration"/> to be registered (via <c>AddMessageBus(...)</c>).
    /// </summary>
    public static IHealthChecksBuilder AddServiceBusHealthCheck(
        this IHealthChecksBuilder builder,
        string name = "servicebus",
        TimeSpan? timeout = null,
        HealthStatus failureStatus = HealthStatus.Unhealthy,
        IEnumerable<string>? tags = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var probeTimeout = timeout ?? TimeSpan.FromSeconds(10);

        return builder.Add(new HealthCheckRegistration(
            name,
            sp => new ServiceBusHealthCheck(
                sp.GetRequiredService<ServiceBusClient>(),
                sp.GetRequiredService<IOptions<MessageBusConfiguration>>(),
                probeTimeout),
            failureStatus,
            tags));
    }
}


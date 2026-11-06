using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using SolTechnology.Core.MessageBus.Broker;
using SolTechnology.Core.MessageBus.Configuration;

namespace SolTechnology.Core.MessageBus.HealthChecks;

/// <summary>
/// Registration extension for the Service Bus connectivity health check on the framework
/// <see cref="IHealthChecksBuilder"/>.
/// </summary>
public static class ServiceBusHealthCheckExtensions
{
    /// <summary>
    /// Adds a Service Bus liveness check. Requires <c>AddSolMessageBus(...)</c>, which registers the
    /// <see cref="ServiceBusClient"/> and <see cref="MessageBusConfiguration"/> the check uses.
    /// </summary>
    public static IHealthChecksBuilder AddSolServiceBusHealthCheck(
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
                sp.GetRequiredKeyedService<ServiceBusClient>(MessageBusClientFactory.ServiceKey),
                sp.GetRequiredService<IOptions<MessageBusConfiguration>>(),
                probeTimeout),
            failureStatus,
            tags));
    }
}


using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using SolTechnology.Core.MessageBus.Configuration;

namespace SolTechnology.Core.MessageBus.HealthChecks;

/// <summary>
/// Lightweight liveness check for the configured Azure Service Bus: creates a short-lived sender
/// on a well-known entity and closes it. Reachable → <see cref="HealthStatus.Healthy"/>,
/// unreachable → the configured failure status. Caller-cancellation is rethrown. Does not
/// consume or send real messages.
/// </summary>
internal sealed class ServiceBusHealthCheck(
    ServiceBusClient client,
    IOptions<MessageBusConfiguration> options,
    TimeSpan timeout) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            // Pick the first configured queue as the probe entity. If none configured, use a
            // well-known probe entity name — the broker rejects unknown entities with a
            // ServiceBusException which we map to Unhealthy below.
            var entityName = options.Value.Queues.FirstOrDefault()?.QueueName ?? "$probe-health";

            // Creating + closing a sender is the cheapest operation that validates connectivity
            // to the broker without consuming messages or requiring Manage claims.
            var sender = client.CreateSender(entityName);
            await sender.CloseAsync(linked.Token);

            return HealthCheckResult.Healthy("Service Bus reachable");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Service Bus unreachable", ex);
        }
    }
}


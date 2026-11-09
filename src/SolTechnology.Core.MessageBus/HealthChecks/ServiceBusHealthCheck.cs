using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using SolTechnology.Core.MessageBus.Configuration;

namespace SolTechnology.Core.MessageBus.HealthChecks;

/// <summary>
/// Connectivity check for the configured Azure Service Bus. Opens a real AMQP link by peeking the
/// first configured queue (non-destructive — no lock/complete, requires only the <i>Listen</i>
/// claim) and round-trips to the broker:
/// <list type="bullet">
///   <item>Peek succeeds → <see cref="HealthStatus.Healthy"/>.</item>
///   <item>Broker answers but rejects the probe (<c>Unauthorized</c> / <c>MessagingEntityNotFound</c>)
///         → <see cref="HealthStatus.Degraded"/> — connectivity is proven, the probe just lacks the
///         claim/entity.</item>
///   <item>No queue configured to probe → <see cref="HealthStatus.Degraded"/> (cannot verify).</item>
///   <item>Connection / DNS / timeout failure → the configured failure status.</item>
/// </list>
/// Caller-cancellation is rethrown. Does not consume or send real messages.
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
        var entityName = options.Value.Queues.FirstOrDefault()?.QueueName;
        if (string.IsNullOrWhiteSpace(entityName))
        {
            // A namespace-level probe needs the Manage claim, so without a queue connectivity stays unverified.
            return HealthCheckResult.Degraded(
                "No queue configured to probe — Service Bus connectivity cannot be verified.");
        }

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            // Peek opens a real AMQP link with only the Listen claim; CreateSender is lazy and proves nothing.
            await using var receiver = client.CreateReceiver(entityName);
            await receiver.PeekMessageAsync(fromSequenceNumber: null, linked.Token);

            return HealthCheckResult.Healthy("Service Bus reachable");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (UnauthorizedAccessException uaex)
        {
            // The broker answered, so connectivity is proven; only the probe's claim is missing.
            return HealthCheckResult.Degraded("Service Bus reachable but probe unauthorized", uaex);
        }
        catch (ServiceBusException sbex) when (
            sbex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
        {
            // The broker answered, so this is a configuration issue, not an outage.
            return HealthCheckResult.Degraded(
                $"Service Bus reachable but probe entity not found: {sbex.Reason}", sbex);
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Service Bus unreachable", ex);
        }
    }
}


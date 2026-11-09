using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.MessageBus.Configuration;

namespace SolTechnology.Core.MessageBus.Broker;

/// <summary>
/// Creates missing queues, topics, and subscriptions when <c>CreateResources</c> is on; does nothing without an
/// administration client. Created queues and subscriptions get the configured <c>MaxDeliveryCount</c>, so the broker
/// and the receiver agree on the last attempt. An entity that another instance creates between the existence check and
/// the create counts as created.
/// </summary>
internal sealed class ServiceBusResourceProvisioner(
    ServiceBusAdministrationClient? administrationClient,
    int maxDeliveryCount,
    ILogger logger)
{
    public async Task Ensure(EndpointRegistration endpoint, CancellationToken ct)
    {
        if (endpoint.Kind == EndpointKind.Queue)
        {
            await EnsureQueue(endpoint.EntityName, ct).ConfigureAwait(false);
            return;
        }

        if (endpoint.Role == EndpointRole.Receiver && string.IsNullOrWhiteSpace(endpoint.SubscriptionName))
        {
            throw new ArgumentException(
                $"Topic receiver for [{endpoint.MessageType.FullName}] requires a subscription name.");
        }

        await EnsureTopic(endpoint.EntityName, ct).ConfigureAwait(false);
        if (endpoint.Role == EndpointRole.Receiver)
        {
            await EnsureSubscription(endpoint.EntityName, endpoint.SubscriptionName!, ct).ConfigureAwait(false);
        }
    }

    private Task EnsureQueue(string queueName, CancellationToken ct) =>
        CreateIfMissing(
            admin => admin.QueueExistsAsync(queueName, ct),
            admin => admin.CreateQueueAsync(new CreateQueueOptions(queueName) { MaxDeliveryCount = maxDeliveryCount }, ct),
            queueName);

    private Task EnsureTopic(string topicName, CancellationToken ct) =>
        CreateIfMissing(
            admin => admin.TopicExistsAsync(topicName, ct),
            admin => admin.CreateTopicAsync(topicName, ct),
            topicName);

    private Task EnsureSubscription(string topicName, string subscriptionName, CancellationToken ct) =>
        CreateIfMissing(
            admin => admin.SubscriptionExistsAsync(topicName, subscriptionName, ct),
            admin => admin.CreateSubscriptionAsync(
                new CreateSubscriptionOptions(topicName, subscriptionName) { MaxDeliveryCount = maxDeliveryCount }, ct),
            $"{topicName}/{subscriptionName}");

    private async Task CreateIfMissing(
        Func<ServiceBusAdministrationClient, Task<Response<bool>>> exists,
        Func<ServiceBusAdministrationClient, Task> create,
        string entityPath)
    {
        if (administrationClient is null || await exists(administrationClient).ConfigureAwait(false))
        {
            return;
        }

        try
        {
            await create(administrationClient).ConfigureAwait(false);
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityAlreadyExists)
        {
            logger.LogDebug("Service Bus entity [{Entity}] was created concurrently by another instance", entityPath);
        }
    }
}

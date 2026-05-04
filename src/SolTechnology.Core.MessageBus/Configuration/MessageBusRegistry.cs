namespace SolTechnology.Core.MessageBus.Configuration;

/// <summary>
/// Internal collector of message-bus endpoint metadata gathered during DI
/// configuration (<c>WithQueuePublisher</c> / <c>WithTopicReceiver</c> etc.).
/// The actual <c>ServiceBusSender</c>/<c>ServiceBusProcessor</c> instances are
/// materialised later, asynchronously, when <see cref="Broker.IMessageBusBroker"/>
/// initialises — this is what allows us to avoid calling
/// <c>services.BuildServiceProvider()</c> from extension methods (ASP0000).
/// </summary>
internal sealed class MessageBusRegistry
{
    private readonly List<EndpointRegistration> _endpoints = new();

    public IReadOnlyList<EndpointRegistration> Endpoints => _endpoints;

    public void Add(EndpointRegistration registration) => _endpoints.Add(registration);
}

internal enum EndpointKind
{
    Queue,
    Topic
}

internal enum EndpointRole
{
    Publisher,
    Receiver
}

internal sealed record EndpointRegistration(
    Type MessageType,
    EndpointKind Kind,
    EndpointRole Role,
    string EntityName,
    string? SubscriptionName);


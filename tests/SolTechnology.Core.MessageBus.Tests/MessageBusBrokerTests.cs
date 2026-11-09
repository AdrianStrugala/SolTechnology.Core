using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using SolTechnology.Core.MessageBus.Broker;
using SolTechnology.Core.MessageBus.Configuration;

namespace SolTechnology.Core.MessageBus.Tests;

[TestFixture]
public class MessageBusBrokerTests
{
    private const string ConnectionString =
        "Endpoint=sb://example.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=v";

    private sealed record QueueMessage(string Id) : IMessage;

    private sealed record TopicMessage(string Id) : IMessage;

    [Test]
    public async Task EnsureInitialized_EntitiesCreatedConcurrentlyByAnotherInstance_StartupContinues()
    {
        // Arrange
        var administration = AdministrationClientThatLosesEveryCreateRace(ServiceBusFailureReason.MessagingEntityAlreadyExists);
        await using var client = new ServiceBusClient(ConnectionString);
        await using var sut = CreateBroker(client, administration);

        // Act
        var act = () => sut.EnsureInitializedAsync();

        // Assert
        await act.Should().NotThrowAsync();
        sut.ResolveReceivers().Select(r => r.MessageType)
            .Should().BeEquivalentTo(new[] { typeof(QueueMessage), typeof(TopicMessage) });
        await administration.Received(1).CreateQueueAsync("orders", Arg.Any<CancellationToken>());
        await administration.Received(1).CreateTopicAsync("events", Arg.Any<CancellationToken>());
        await administration.Received(1).CreateSubscriptionAsync("events", "billing", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EnsureInitialized_CreateFailsForAnotherReason_Throws()
    {
        // Arrange
        var administration = AdministrationClientThatLosesEveryCreateRace(ServiceBusFailureReason.QuotaExceeded);
        await using var client = new ServiceBusClient(ConnectionString);
        await using var sut = CreateBroker(client, administration);

        // Act
        var act = () => sut.EnsureInitializedAsync();

        // Assert
        (await act.Should().ThrowAsync<ServiceBusException>())
            .Which.Reason.Should().Be(ServiceBusFailureReason.QuotaExceeded);
    }

    private static MessageBusBroker CreateBroker(ServiceBusClient client, ServiceBusAdministrationClient administration)
    {
        var registry = new MessageBusRegistry();
        registry.Add(new EndpointRegistration(typeof(QueueMessage), EndpointKind.Queue, EndpointRole.Receiver, "orders", null));
        registry.Add(new EndpointRegistration(typeof(TopicMessage), EndpointKind.Topic, EndpointRole.Receiver, "events", "billing"));
        var options = Options.Create(new MessageBusConfiguration { ConnectionString = ConnectionString, CreateResources = true });
        return new MessageBusBroker(options, registry, client, NullLogger<MessageBusBroker>.Instance, administration);
    }

    private static ServiceBusAdministrationClient AdministrationClientThatLosesEveryCreateRace(ServiceBusFailureReason reason)
    {
        var administration = Substitute.For<ServiceBusAdministrationClient>();
        var notExists = Task.FromResult(Response.FromValue(false, Substitute.For<Response>()));
        var failure = new ServiceBusException("entity create failed", reason);
        administration.QueueExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(notExists);
        administration.TopicExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(notExists);
        administration.SubscriptionExistsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(notExists);
        administration.CreateQueueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ThrowsAsync(failure);
        administration.CreateTopicAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ThrowsAsync(failure);
        administration.CreateSubscriptionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).ThrowsAsync(failure);
        return administration;
    }
}

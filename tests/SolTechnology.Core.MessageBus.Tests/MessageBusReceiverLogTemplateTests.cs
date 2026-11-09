using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using SolTechnology.Core.MessageBus.Publish;
using SolTechnology.Core.MessageBus.Receive;
using static SolTechnology.Core.MessageBus.Tests.ReceiverTestHost;

namespace SolTechnology.Core.MessageBus.Tests;

[TestFixture]
public class MessageBusReceiverLogTemplateTests
{
    private const string ProcessorErrorTemplate =
        "Service Bus processor error on [{Entity}] ([{Source}]/[{Namespace}]): [{Message}]";

    private static readonly Regex UnbracketedPlaceholder = new(@"(?<!\[)\{[A-Za-z]+\}|\{[A-Za-z]+\}(?!\])");

    [Test]
    public async Task PublishReceiveAndStop_LogTemplates_BracketEveryPlaceholder()
    {
        // Arrange
        const string queue = "receiver-complete";
        var logs = new CapturingLoggerProvider();
        var probe = new HandlerProbe(_ => Task.CompletedTask);
        await using var host = await StartHost(queue, probe, maxDeliveryCount: 10, logs);
        var receiver = host.Services.GetServices<IHostedService>().OfType<MessageBusReceiver>().Single();

        // Act
        await host.Services.GetRequiredService<IMessagePublisher>().Publish(new ReceiverMessage(Guid.NewGuid().ToString(), "logs"));
        await Eventually(() => Task.FromResult(HasTemplate(logs, "Handled message [{MessageType}] [{MessageId}] (delivery [{DeliveryCount}])")));
        await receiver.StopAsync(new CancellationToken(canceled: true));

        // Assert
        Templates(logs).Should().Contain(new[]
        {
            "Starting Service Bus processor for [{MessageType}] on [{Entity}]",
            "Published message [{MessageType}] [{MessageId}] to [{Entity}]",
            "Handled message [{MessageType}] [{MessageId}] (delivery [{DeliveryCount}])",
            "Failed to stop Service Bus processor on [{Entity}]"
        });
        Templates(logs).Should().AllSatisfy(template => UnbracketedPlaceholder.IsMatch(template).Should().BeFalse(template));
    }

    [Test]
    public async Task MissingQueue_LogTemplates_BracketEveryPlaceholder()
    {
        // Arrange
        const string queue = "receiver-missing";
        var logs = new CapturingLoggerProvider();
        await using var host = await StartHost(queue, new HandlerProbe(_ => Task.CompletedTask), maxDeliveryCount: 10, logs);

        // Act
        var publish = () => host.Services.GetRequiredService<IMessagePublisher>()
            .Publish(new ReceiverMessage(Guid.NewGuid().ToString(), "missing"));

        // Assert
        await publish.Should().ThrowAsync<Exception>();
        (await Eventually(() => Task.FromResult(HasTemplate(logs, ProcessorErrorTemplate)))).Should().BeTrue();
        Templates(logs).Should().Contain("Failed to publish message [{MessageType}] [{MessageId}] to [{Entity}]");
        Templates(logs).Should().AllSatisfy(template => UnbracketedPlaceholder.IsMatch(template).Should().BeFalse(template));
    }

    private static IReadOnlyCollection<string> Templates(CapturingLoggerProvider logs) =>
        logs.Entries
            .Where(entry => entry.Category.StartsWith("SolTechnology.Core.MessageBus", StringComparison.Ordinal))
            .Select(entry => entry.Template)
            .OfType<string>()
            .Distinct()
            .ToArray();

    private static bool HasTemplate(CapturingLoggerProvider logs, string template) => Templates(logs).Contains(template);
}

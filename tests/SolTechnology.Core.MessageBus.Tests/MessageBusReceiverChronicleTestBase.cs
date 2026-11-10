using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using SolTechnology.Core.Logging;
using SolTechnology.Core.Logging.Chronicles;
using SolTechnology.Core.MessageBus.Broker;
using SolTechnology.Core.MessageBus.Configuration;
using SolTechnology.Core.MessageBus.Receive;

namespace SolTechnology.Core.MessageBus.Tests;

/// <summary>Calls <c>MessageBusReceiver.HandleMessageAsync</c> directly with a real Chronicle; no emulator.</summary>
public abstract class MessageBusReceiverChronicleTestBase
{
    private ServiceProvider _provider = null!;
    private MessageBusReceiver _sut = null!;

    public sealed record ProbeMessage(string Id, string Mode) : IMessage;

    /// <summary>Handles <see cref="ProbeMessage"/> by its <c>Mode</c>: ok, throw, cancel, step, or wait.</summary>
    public sealed class ProbeHandler(IChronicle chronicle, ProbeGate gate) : IMessageHandler<ProbeMessage>
    {
        public async Task Handle(ProbeMessage message, CancellationToken cancellationToken)
        {
            using var step = chronicle.Step("SaveOrder");
            switch (message.Mode)
            {
                case "throw":
                    throw new InvalidOperationException("boom");
                case "cancel":
                    cancellationToken.ThrowIfCancellationRequested();
                    break;
                case "wait":
                    await gate.Arrive();
                    break;
            }
        }
    }

    /// <summary>Releases its waiters only when two handlers have arrived, so both run concurrently.</summary>
    public sealed class ProbeGate
    {
        private readonly TaskCompletionSource _bothArrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrived;

        public Task Arrive()
        {
            if (Interlocked.Increment(ref _arrived) == 2)
            {
                _bothArrived.TrySetResult();
            }

            return _bothArrived.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
    }

    protected FakeLogCollector Logs { get; private set; } = null!;

    protected IReadOnlyList<FakeLogRecord> Reports => [.. Logs.GetSnapshot().Where(r => r.Id.Id == 2154)];

    [SetUp]
    public void SetUpReceiver()
    {
        Logs = new FakeLogCollector();
        var services = new ServiceCollection()
            .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(new FakeLoggerProvider(Logs)))
            .AddSingleton<ProbeGate>()
            .AddScoped<IMessageHandler<ProbeMessage>, ProbeHandler>()
            .AddSolCorrelationIdService();
        services.AddSolChronicle();
        _provider = services.BuildServiceProvider();
        _sut = new MessageBusReceiver(
            new NoReceiversBroker(),
            _provider,
            _provider.GetRequiredService<SolTechnology.Core.Logging.Correlations.ICorrelationIdService>(),
            Options.Create(new MessageBusConfiguration { MaxDeliveryCount = 3 }),
            _provider.GetRequiredService<IChronicle>(),
            _provider.GetRequiredService<ILogger<MessageBusReceiver>>());
    }

    [TearDown]
    public async Task TearDownReceiver()
    {
        await _sut.DisposeAsync();
        await _provider.DisposeAsync();
    }

    protected Task Handle(string body, int deliveryCount = 1, bool completeFails = false, CancellationToken cancellationToken = default)
    {
        var receiver = Substitute.For<ServiceBusReceiver>();
        receiver.EntityPath.Returns("orders");
        if (completeFails)
        {
            receiver.CompleteMessageAsync(Arg.Any<ServiceBusReceivedMessage>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new ServiceBusException("lock lost", ServiceBusFailureReason.MessageLockLost));
        }

        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString(body), messageId: $"message-{deliveryCount}-{body.Length}", deliveryCount: deliveryCount);
        return _sut.HandleMessageAsync(new ProcessMessageEventArgs(message, receiver, cancellationToken), typeof(ProbeMessage));
    }

    private sealed class NoReceiversBroker : IMessageBusBroker
    {
        public Task EnsureInitializedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public IReadOnlyList<ServiceBusSender> ResolveSenders(Type messageType) => [];

        public IReadOnlyList<(Type MessageType, ServiceBusProcessor Processor)> ResolveReceivers() => [];
    }

    protected static string Body(string mode) => $"{{\"Id\":\"secret-body-id\",\"Mode\":\"{mode}\"}}";
}

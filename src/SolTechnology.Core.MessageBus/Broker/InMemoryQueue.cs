using System.Threading.Channels;
using Azure.Messaging.ServiceBus;
using SolTechnology.Core.MessageBus.Publish;
using SolTechnology.Core.MessageBus.Receive;

namespace SolTechnology.Core.MessageBus.Broker;

public class InMemoryQueue : ISender, IReceiver
{
    private readonly Channel<IMessage> _channel;
    private Func<ProcessMessageEventArgs, Task> _messageHandler;

    public string QueueName { get; }

    public InMemoryQueue(string name)
    {
        QueueName = name;
        _channel = Channel.CreateUnbounded<IMessage>(new UnboundedChannelOptions { SingleReader = true });
    }
    public async Task Send(IMessage message)
    {
        await _channel.Writer.WriteAsync(message);
    }

    public async Task<IMessage> DequeueAsync(CancellationToken cancellationToken = default)
    {
        return await _channel.Reader.ReadAsync(cancellationToken);
    }

    public Task Close()
    {
        return Task.CompletedTask;
    }

    public void AssignMessageHandler(Func<ProcessMessageEventArgs, Task> func)
    {
        _messageHandler = func;
    }

    public void AssignErrorHandler(Func<ProcessErrorEventArgs, Task> func)
    {
        throw new NotImplementedException();
    }

    public async Task StartProcessingAsync(CancellationToken cancellationToken = default)
    {
        var message = await _channel.Reader.ReadAsync(cancellationToken);
    }

    public Task StopProcessingAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
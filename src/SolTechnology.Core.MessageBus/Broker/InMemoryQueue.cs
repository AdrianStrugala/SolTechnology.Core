using System.Threading.Channels;
using SolTechnology.Core.MessageBus.Publish;
using SolTechnology.Core.MessageBus.Receive;

namespace SolTechnology.Core.MessageBus.Broker;

public class InMemoryQueue : ISender, IReceiver
{
    private readonly Channel<IMessage> _channel;
    private Func<IMessage, CancellationToken, Type, Task> _messageHandler;
    private Func<Exception, Task> _errorHandler;
    private bool _shouldBeProcessing = true;

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

    public Task Close()
    {
        _channel.Writer.TryComplete();
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
     
        return ValueTask.CompletedTask;
    }

    public void AssignMessageHandler(Func<IMessage, CancellationToken, Type, Task> func, Type type)
    {
        _messageHandler = func;
    }

    public void AssignErrorHandler(Func<Exception, Task> func)
    {
        _errorHandler = func;
    }

    public async Task StartProcessingAsync(CancellationToken cancellationToken = default)
    {
        while (_shouldBeProcessing)
        {
            var message = await _channel.Reader.ReadAsync(cancellationToken);
            await _messageHandler(message, cancellationToken, message.GetType());
        }
    }

    public Task StopProcessingAsync(CancellationToken cancellationToken = default)
    {
        _shouldBeProcessing = false;
        return Task.CompletedTask;
    }
}
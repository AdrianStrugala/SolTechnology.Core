using System.Threading.Channels;
using SolTechnology.Core.MessageBus.Publish;
using SolTechnology.Core.MessageBus.Receive;

namespace SolTechnology.Core.MessageBus.Broker;

public class InMemoryQueue : ISender, IReceiver
{
    private readonly Channel<IMessage> _channel;
    private Func<IMessage, CancellationToken, Type, Task> _messageHandler;
    private Func<Exception, Task> _errorHandler;
    private CancellationTokenSource _cancellationTokenSource;

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

    public Task StartProcessingAsync(CancellationToken cancellationToken = default)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        cancellationToken = _cancellationTokenSource.Token;

        Task longRunningTask = Task.Run(async () =>
        {
            var message = await _channel.Reader.ReadAsync(cancellationToken);

            try
            {
                await _messageHandler(message, cancellationToken, message.GetType());
            }
            catch (Exception e)
            {
                await _errorHandler(e);
            }
        }, cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopProcessingAsync(CancellationToken cancellationToken = default)
    {
        _cancellationTokenSource.Cancel();
        return Task.CompletedTask;
    }

    public async Task CloseAsync()
    {
        await StopProcessingAsync();
    }
}
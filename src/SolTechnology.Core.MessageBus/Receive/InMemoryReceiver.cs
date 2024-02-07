using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SolTechnology.Core.MessageBus.Broker;

public class InMemoryReceiver : IHostedService
{
    private readonly IMessageBusBroker _broker;
    private readonly List<InMemoryQueue> _messageQueues;
    private List<Task> _backgroundTasks;
    private CancellationTokenSource _cancellationTokenSource;

    public InMemoryReceiver(IMessageBusBroker broker)
    {
        _broker = broker;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _messageQueues = _broker.ResolveMessageReceivers();
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _backgroundTasks = new List<Task>();

        foreach (var queue in _messageQueues)
        {
            _backgroundTasks.Add(Task.Run(async () => await ProcessMessages(queue, _cancellationTokenSource.Token)));
        }

        return Task.CompletedTask;
    }

    private async Task ProcessMessages(MessageQueue<string> queue, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var message = await queue.DequeueAsync(cancellationToken);
                Console.WriteLine($"Processing message from {queue.QueueName}: {message}");
                // Implement your message processing logic here
            }
        }
        catch (OperationCanceledException)
        {
            // Expected to catch this exception when the cancellation token is cancelled
            Console.WriteLine($"Message processing cancelled for {queue.QueueName}.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource.Cancel();
        return Task.WhenAll(_backgroundTasks);
    }
}
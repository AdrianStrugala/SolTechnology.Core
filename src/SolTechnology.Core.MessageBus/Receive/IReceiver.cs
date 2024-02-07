using Azure.Messaging.ServiceBus;

namespace SolTechnology.Core.MessageBus.Receive
{
    public interface IReceiver
    {
        public void AssignMessageHandler(Func<ProcessMessageEventArgs, Task> func);

        public void AssignErrorHandler(Func<ProcessErrorEventArgs, Task> func);

        public Task StartProcessingAsync(CancellationToken cancellationToken = default);

        public Task StopProcessingAsync(CancellationToken cancellationToken = default);

    }
}

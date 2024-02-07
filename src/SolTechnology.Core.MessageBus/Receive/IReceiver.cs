namespace SolTechnology.Core.MessageBus.Receive
{
    public interface IReceiver : IAsyncDisposable
    {
        public void AssignMessageHandler(Func<IMessage, CancellationToken, Type, Task> func, Type type);

        public void AssignErrorHandler(Func<Exception, Task> func);

        public Task StartProcessingAsync(CancellationToken cancellationToken = default);

        public Task StopProcessingAsync(CancellationToken cancellationToken = default);

        Task CloseAsync();
    }
}

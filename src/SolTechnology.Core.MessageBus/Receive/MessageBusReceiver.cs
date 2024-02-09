using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.MessageBus.Broker;

namespace SolTechnology.Core.MessageBus.Receive
{

    public class MessageBusReceiver : IHostedService, IAsyncDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MessageBusReceiver> _logger;

        private readonly IMessageBusBroker _messageBusBroker;
        private List<IReceiver> _processors;


        public MessageBusReceiver(
           IMessageBusBroker messageBusBroker,
           IServiceProvider serviceProvider,
           ILogger<MessageBusReceiver> logger)
        {
            _messageBusBroker = messageBusBroker;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var typeProcessorMap = _messageBusBroker.ResolveMessageReceivers();
            _processors = typeProcessorMap.Select(x => x.Item2).ToList();

            foreach (var (messageType, receiver) in typeProcessorMap)
            {
                _logger.LogInformation($"Starting message bus processor for: [{messageType.ToString()}]");

                receiver.AssignErrorHandler(HandleError);
                receiver.AssignMessageHandler(HandleMessageAsync, messageType);

                await receiver.StartProcessingAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (IReceiver processor in _processors)
            {
                await processor.StopProcessingAsync(cancellationToken).ConfigureAwait(false);
            }
        }


        private async Task HandleMessageAsync(IMessage message, CancellationToken token, Type messageType)
        {
            using IServiceScope scope = _serviceProvider.CreateScope();


            var x = Type.MakeGenericSignatureType(typeof(IMessageHandler<>), messageType);
            var handlerType = typeof(IMessageHandler<>).MakeGenericType(messageType);

            dynamic handler = scope.ServiceProvider.GetRequiredService(handlerType);
            await handler.Handle(message, token);
        }

        protected virtual Task HandleError(Exception exception)
        {
            _logger.LogError(exception, exception.Message);
            return Task.CompletedTask;
        }


        public async ValueTask DisposeAsync()
        {
            if (_processors != null)
            {
                foreach (var processor in _processors)
                {
                    await processor.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }
}

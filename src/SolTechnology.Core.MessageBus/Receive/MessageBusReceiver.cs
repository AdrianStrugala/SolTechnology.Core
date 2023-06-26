using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SolTechnology.Core.MessageBus.Broker;

namespace SolTechnology.Core.MessageBus.Receive
{

    public class MessageBusReceiver : IHostedService, IAsyncDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MessageBusReceiver> _logger;

        private readonly IMessageBusBroker _messageBusBroker;
        private List<ServiceBusProcessor> _processors;


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

            foreach (var (messageType, processor) in typeProcessorMap)
            {
                _logger.LogInformation($"Starting message bus processor for: [{processor.EntityPath}]");
                processor.ProcessErrorAsync += HandleError;
                processor.ProcessMessageAsync += async args =>
                {
                    await HandleMessageAsync(args, messageType);
                };

                await processor.StartProcessingAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (ServiceBusProcessor processor in _processors)
            {
                await processor.StopProcessingAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task HandleMessageAsync(ProcessMessageEventArgs args, Type messageType)
        {
            using IServiceScope scope = _serviceProvider.CreateScope();


            var x = Type.MakeGenericSignatureType(typeof(IMessageHandler<>), messageType);
            var handlerType = typeof(IMessageHandler<>).MakeGenericType(messageType);

            dynamic handler = scope.ServiceProvider.GetRequiredService(handlerType);
            dynamic message = JsonConvert.DeserializeObject(args.Message?.Body.ToString(), messageType);
            await handler.Handle(message, args.CancellationToken);
        }

        // protected virtual async Task HandleMessageAsync(ProcessMessageEventArgs args)
        // {
        //     using IServiceScope scope = _serviceProvider.CreateScope();
        //
        //     var HandleMessageAsync = scope.ServiceProvider.GetRequiredService<IMessageHandler<TMessage>>();
        //     var message = JsonConvert.DeserializeObject<TMessage>(args.Message?.Body.ToString());
        //     await HandleMessageAsync.Handle(message, args.CancellationToken);
        // }


        protected virtual Task HandleError(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception, args.Exception.Message);
            return Task.CompletedTask;
        }


        public async ValueTask DisposeAsync()
        {
            if (_processors != null)
            {
                foreach (ServiceBusProcessor processor in _processors)
                {
                    await processor.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }
}

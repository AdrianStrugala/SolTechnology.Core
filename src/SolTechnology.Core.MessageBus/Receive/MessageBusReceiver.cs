using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SolTechnology.Core.MessageBus.Configuration;
using SolTechnology.Core.MessageBus.Publish;

namespace SolTechnology.Core.MessageBus.Receive
{

    public class MessageBusReceiver<TMessage> : IHostedService, IAsyncDisposable where TMessage : IMessage
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MessageBusReceiver<TMessage>> _logger;

        private readonly IMessageBusConfigurationProvider _messageBusConfigurationProvider;
        private List<ServiceBusProcessor> _processors;


        public MessageBusReceiver(
           IMessageBusConfigurationProvider messageBusConfigurationProvider,
           IServiceProvider serviceProvider,
           ILogger<MessageBusReceiver<TMessage>> logger)
        {
            _messageBusConfigurationProvider = messageBusConfigurationProvider;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _processors = _messageBusConfigurationProvider.ResolveMessageReceiver(typeof(TMessage).Name);

            foreach (ServiceBusProcessor processor in _processors)
            {
                _logger.LogInformation($"Starting message bus processor for topic: [{processor.EntityPath}]");
                processor.ProcessMessageAsync += HandleMessageAsync;
                processor.ProcessErrorAsync += HandleError;

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


        protected virtual async Task HandleMessageAsync(ProcessMessageEventArgs args)
        {
            try
            {
                using IServiceScope scope = _serviceProvider.CreateScope();
                var handler = (IMessageHandler<TMessage>)scope.ServiceProvider.GetRequiredService(GetType());
                var message = JsonConvert.DeserializeObject<TMessage>(args.Message?.Body.ToString());
                await handler.Handle(message, args.CancellationToken);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                await args.DeadLetterMessageAsync(args.Message);
                throw;
            }
        }

        protected virtual Task HandleError(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception, args.Exception.Message);
            return Task.CompletedTask;
        }


        public async ValueTask DisposeAsync()
        {
            if (_processors != null)
                foreach (ServiceBusProcessor processor in _processors)
                {
                    await processor.DisposeAsync().ConfigureAwait(false);
                }
        }
    }
}

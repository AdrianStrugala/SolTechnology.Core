using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;

namespace SolTechnology.Core.MessageBus.Receive
{
    public class AzureReceiver : IReceiver
    {
        private readonly ServiceBusProcessor _processor;

        public AzureReceiver(ServiceBusProcessor processor)
        {
            _processor = processor;
        }

        public void AssignMessageHandler(Func<IMessage, CancellationToken, Type, Task> func, Type type)
        {
            Task HandleMessage(ProcessMessageEventArgs args) => func((IMessage)JsonConvert.DeserializeObject(args.Message?.Body.ToString(),type), args.CancellationToken, type);
            _processor.ProcessMessageAsync += HandleMessage;
        }

        public void AssignErrorHandler(Func<Exception, Task> func)
        {
            Task HandleError(ProcessErrorEventArgs args) => func(args.Exception);

            _processor.ProcessErrorAsync += HandleError;
        }

        public async Task StartProcessingAsync(CancellationToken cancellationToken = default)
        {
            await _processor.StartProcessingAsync(cancellationToken);
        }

        public async Task StopProcessingAsync(CancellationToken cancellationToken = default)
        {
            await _processor.StopProcessingAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task CloseAsync()
        {
            await _processor.CloseAsync();
        }

        public async ValueTask DisposeAsync()
        {
            if (_processor != null) await _processor.DisposeAsync();
        }
    }
}

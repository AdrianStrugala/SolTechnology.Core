using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace SolTechnology.Core.MessageBus.Receive
{
    public class AzureReceiver : IReceiver
    {
        private readonly ServiceBusProcessor _processor;

        public AzureReceiver(ServiceBusProcessor processor)
        {
            _processor = processor;
        }

        public void AssignMessageHandler(Func<ProcessMessageEventArgs, Task> func)
        {
            _processor.ProcessMessageAsync += func;
        }

        public void AssignErrorHandler(Func<ProcessErrorEventArgs, Task> func)
        {
            _processor.ProcessErrorAsync += func;
        }

        public Task StartProcessingAsync(CancellationToken cancellationToken = default)
        {
            return _processor.StartProcessingAsync(cancellationToken);
        }

        public Task StopProcessingAsync(CancellationToken cancellationToken = default)
        {
            return _processor.StartProcessingAsync(cancellationToken);
        }
    }
}

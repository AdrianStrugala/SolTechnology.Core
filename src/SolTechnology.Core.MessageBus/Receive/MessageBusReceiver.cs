﻿using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SolTechnology.Core.Logging.Correlations;
using SolTechnology.Core.MessageBus.Broker;
using SolTechnology.Core.MessageBus.Configuration;

namespace SolTechnology.Core.MessageBus.Receive
{
    /// <summary>
    /// Hosted service that drives every <see cref="ServiceBusProcessor"/> registered
    /// with <c>WithQueueReceiver</c>/<c>WithTopicReceiver</c>. Failures inside a
    /// handler are caught, logged and abandoned for retry, or dead-lettered with reason
    /// <c>HandlerFailedMaxDelivery</c> once the delivery count reaches
    /// <see cref="MessageBusConfiguration.MaxDeliveryCount"/> — the processor is configured with
    /// <c>AutoCompleteMessages = false</c> so the receiver always owns the lock
    /// settlement decision.
    /// </summary>
    internal sealed class MessageBusReceiver(
        IMessageBusBroker messageBusBroker,
        IServiceProvider serviceProvider,
        ICorrelationIdService correlationIdService,
        IOptions<MessageBusConfiguration> options,
        ILogger<MessageBusReceiver> logger) : IHostedService, IAsyncDisposable
    {
        private readonly ConcurrentBag<ServiceBusProcessor> _ownedProcessors = new();
        private readonly HandlerInvoker _handlerInvoker = new();
        private readonly MessageSettler _settler = new(logger, options.Value.MaxDeliveryCount);

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await messageBusBroker.EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

            foreach (var (messageType, processor) in messageBusBroker.ResolveReceivers())
            {
                logger.LogInformation(
                    "Starting Service Bus processor for {MessageType} on {Entity}",
                    messageType.FullName, processor.EntityPath);

                var capturedType = messageType;
                processor.ProcessErrorAsync += HandleErrorAsync;
                processor.ProcessMessageAsync += args => HandleMessageAsync(args, capturedType);

                _ownedProcessors.Add(processor);
                await processor.StartProcessingAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var processor in _ownedProcessors)
            {
                try
                {
                    await processor.StopProcessingAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex,
                        "Failed to stop Service Bus processor on {Entity}", processor.EntityPath);
                }
            }
        }

        private async Task HandleMessageAsync(ProcessMessageEventArgs args, Type messageType)
        {
            RestoreCorrelation(args.Message);

            using var logScope = logger.BeginScope(new Dictionary<string, object?>
            {
                ["CorrelationId"] = correlationIdService.Get()?.Value,
                ["MessageId"] = args.Message.MessageId,
                ["MessageType"] = messageType.Name
            });

            try
            {
                var message = MessageDeserializer.Deserialize(args.Message.Body, messageType);
                if (message.Payload is null)
                {
                    await _settler.DeadLetterUnreadable(args, messageType, message).ConfigureAwait(false);
                    return;
                }

                await using var scope = serviceProvider.CreateAsyncScope();
                await _handlerInvoker.Invoke(scope.ServiceProvider, messageType, message.Payload, args.CancellationToken)
                    .ConfigureAwait(false);
                await _settler.Complete(args, messageType).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (args.CancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                await _settler.SettleFailure(args, messageType, ex).ConfigureAwait(false);
            }
        }

        private void RestoreCorrelation(ServiceBusReceivedMessage message)
        {
            if (message.ApplicationProperties.TryGetValue("CorrelationId", out var rawCorrelation)
                && rawCorrelation is string correlationValue
                && !string.IsNullOrWhiteSpace(correlationValue))
            {
                correlationIdService.Set(CorrelationId.FromString(correlationValue));
            }
            else
            {
                correlationIdService.GetOrGenerate();
            }
        }

        private Task HandleErrorAsync(ProcessErrorEventArgs args)
        {
            logger.LogError(args.Exception,
                "Service Bus processor error on {Entity} ({Source}/{Namespace}): {Message}",
                args.EntityPath, args.ErrorSource, args.FullyQualifiedNamespace, args.Exception.Message);
            return Task.CompletedTask;
        }

        // Processors are disposed by the broker; disposing them here too would double-dispose.
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

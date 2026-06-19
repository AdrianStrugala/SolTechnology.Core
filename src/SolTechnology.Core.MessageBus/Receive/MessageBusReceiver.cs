﻿using System.Collections.Concurrent;
using System.Text;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SolTechnology.Core.Logging.Correlations;
using SolTechnology.Core.MessageBus.Broker;

namespace SolTechnology.Core.MessageBus.Receive
{
    /// <summary>
    /// Hosted service that drives every <see cref="ServiceBusProcessor"/> registered
    /// with <c>WithQueueReceiver</c>/<c>WithTopicReceiver</c>. Failures inside a
    /// handler are caught, logged and either abandoned (transient) or dead-lettered
    /// (last delivery attempt) — the processor is configured with
    /// <c>AutoCompleteMessages = false</c> so the receiver always owns the lock
    /// settlement decision.
    /// </summary>
    public sealed class MessageBusReceiver(
        IMessageBusBroker messageBusBroker,
        IServiceProvider serviceProvider,
        ICorrelationIdService correlationIdService,
        ILogger<MessageBusReceiver> logger) : IHostedService, IAsyncDisposable
    {
        private readonly ConcurrentBag<ServiceBusProcessor> _ownedProcessors = new();
        private readonly ConcurrentDictionary<Type, Func<IServiceProvider, object, CancellationToken, Task>>
            _invokersByType = new();


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
            var messageId = args.Message.MessageId;
            var deliveryCount = args.Message.DeliveryCount;

            // Propagate correlation from the publisher so all handler logs are traceable.
            if (args.Message.ApplicationProperties.TryGetValue("CorrelationId", out var rawCorrelation)
                && rawCorrelation is string correlationValue
                && !string.IsNullOrWhiteSpace(correlationValue))
            {
                correlationIdService.Set(CorrelationId.FromString(correlationValue));
            }
            else
            {
                correlationIdService.GetOrGenerate();
            }

            using var logScope = logger.BeginScope(new Dictionary<string, object?>
            {
                ["CorrelationId"] = correlationIdService.Get()?.Value,
                ["MessageId"] = messageId,
                ["MessageType"] = messageType.Name
            });

            try
            {
                var body = Encoding.UTF8.GetString(args.Message.Body);
                if (string.IsNullOrWhiteSpace(body))
                {
                    logger.LogError(
                        "Dead-lettering empty message {MessageId} for {MessageType}",
                        messageId, messageType.FullName);
                    await args.DeadLetterMessageAsync(args.Message, "EmptyBody",
                        "Message body is empty.", args.CancellationToken).ConfigureAwait(false);
                    return;
                }

                object? payload;
                try
                {
                    payload = JsonConvert.DeserializeObject(body, messageType);
                }
                catch (JsonException jsonEx)
                {
                    logger.LogError(jsonEx,
                        "Dead-lettering message {MessageId} — deserialisation to {MessageType} failed",
                        messageId, messageType.FullName);
                    await args.DeadLetterMessageAsync(args.Message, "DeserializationFailed",
                        jsonEx.Message, args.CancellationToken).ConfigureAwait(false);
                    return;
                }

                if (payload is null)
                {
                    logger.LogError(
                        "Dead-lettering message {MessageId} — null payload after deserialisation to {MessageType}",
                        messageId, messageType.FullName);
                    await args.DeadLetterMessageAsync(args.Message, "NullPayload",
                        "Deserialised payload is null.", args.CancellationToken).ConfigureAwait(false);
                    return;
                }

                await using var scope = serviceProvider.CreateAsyncScope();
                var invoker = _invokersByType.GetOrAdd(messageType, BuildInvoker);
                await invoker(scope.ServiceProvider, payload, args.CancellationToken).ConfigureAwait(false);

                await args.CompleteMessageAsync(args.Message, args.CancellationToken).ConfigureAwait(false);

                logger.LogDebug(
                    "Handled message {MessageType} {MessageId} (delivery {DeliveryCount})",
                    messageType.Name, messageId, deliveryCount);
            }
            catch (OperationCanceledException) when (args.CancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Handler failure for {MessageType} {MessageId} (delivery {DeliveryCount}) — abandoning for retry",
                    messageType.Name, messageId, deliveryCount);
                try
                {
                    await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception abandonEx)
                {
                    logger.LogWarning(abandonEx,
                        "Failed to abandon message {MessageId}; lock will expire naturally", messageId);
                }
            }
        }

        private Task HandleErrorAsync(ProcessErrorEventArgs args)
        {
            logger.LogError(args.Exception,
                "Service Bus processor error on {Entity} ({Source}/{Namespace}): {Message}",
                args.EntityPath, args.ErrorSource, args.FullyQualifiedNamespace, args.Exception.Message);
            return Task.CompletedTask;
        }

        private static Func<IServiceProvider, object, CancellationToken, Task> BuildInvoker(Type messageType)
        {
            var handlerType = typeof(IMessageHandler<>).MakeGenericType(messageType);
            var handleMethod = handlerType.GetMethod(nameof(IMessageHandler<IMessage>.Handle))
                ?? throw new InvalidOperationException(
                    $"Handler {handlerType.FullName} does not expose a Handle method.");

            return (sp, message, ct) =>
            {
                var handler = sp.GetRequiredService(handlerType);
                return (Task)handleMethod.Invoke(handler, new[] { message, ct })!;
            };
        }

        public async ValueTask DisposeAsync()
        {
            // Disposal of processors is owned by the broker — we only stop them here.
            // Avoid double-dispose (broker.DisposeAsync also disposes processors).
            await Task.CompletedTask;
        }
    }
}

using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.MessageBus.Receive;

/// <summary>
/// Completes, abandons, or dead-letters a received message and logs the decision under the receiver's category.
/// A failing message is dead-lettered with reason <c>HandlerFailedMaxDelivery</c> once its delivery count reaches
/// <paramref name="maxDeliveryCount"/>. Settlement ignores <see cref="ProcessMessageEventArgs.CancellationToken"/>,
/// which the processor cancels as soon as shutdown starts, so a message handled during shutdown is still settled.
/// </summary>
internal sealed class MessageSettler(ILogger logger, int maxDeliveryCount)
{
    private const string HandlerFailedMaxDelivery = "HandlerFailedMaxDelivery";

    public async Task Complete(ProcessMessageEventArgs args, Type messageType)
    {
        await args.CompleteMessageAsync(args.Message, CancellationToken.None).ConfigureAwait(false);

        logger.LogDebug(
            "Handled message [{MessageType}] [{MessageId}] (delivery [{DeliveryCount}])",
            messageType.Name, args.Message.MessageId, args.Message.DeliveryCount);
    }

    public Task DeadLetterUnreadable(ProcessMessageEventArgs args, Type messageType, DeserializedMessage message)
    {
        var messageId = args.Message.MessageId;
        switch (message.Failure)
        {
            case DeserializationFailure.EmptyBody:
                logger.LogError(
                    "Dead-lettering empty message [{MessageId}] for [{MessageType}]",
                    messageId, messageType.FullName);
                return DeadLetter(args, "EmptyBody", "Message body is empty.");
            case DeserializationFailure.InvalidJson:
                logger.LogError(message.Error,
                    "Dead-lettering message [{MessageId}] — deserialisation to [{MessageType}] failed",
                    messageId, messageType.FullName);
                return DeadLetter(args, "DeserializationFailed", message.Error!.Message);
            default:
                logger.LogError(
                    "Dead-lettering message [{MessageId}] — null payload after deserialisation to [{MessageType}]",
                    messageId, messageType.FullName);
                return DeadLetter(args, "NullPayload", "Deserialised payload is null.");
        }
    }

    public async Task SettleFailure(ProcessMessageEventArgs args, Type messageType, Exception exception)
    {
        var messageId = args.Message.MessageId;
        var deliveryCount = args.Message.DeliveryCount;
        if (deliveryCount >= maxDeliveryCount)
        {
            logger.LogError(exception,
                "Handler failure for [{MessageType}] [{MessageId}] (delivery [{DeliveryCount}]) — dead-lettering on the last attempt",
                messageType.Name, messageId, deliveryCount);
            await TrySettle(
                () => DeadLetter(args, HandlerFailedMaxDelivery, exception.Message),
                settleException => logger.LogWarning(settleException,
                    "Failed to dead-letter message [{MessageId}]; lock will expire naturally", messageId)).ConfigureAwait(false);
            return;
        }

        logger.LogError(exception,
            "Handler failure for [{MessageType}] [{MessageId}] (delivery [{DeliveryCount}]) — abandoning for retry",
            messageType.Name, messageId, deliveryCount);
        await TrySettle(
            () => args.AbandonMessageAsync(args.Message, cancellationToken: CancellationToken.None),
            settleException => logger.LogWarning(settleException,
                "Failed to abandon message [{MessageId}]; lock will expire naturally", messageId)).ConfigureAwait(false);
    }

    private static Task DeadLetter(ProcessMessageEventArgs args, string reason, string description) =>
        args.DeadLetterMessageAsync(args.Message, reason, description, CancellationToken.None);

    private static async Task TrySettle(Func<Task> settle, Action<Exception> logFailure)
    {
        try
        {
            await settle().ConfigureAwait(false);
        }
        catch (Exception settleException)
        {
            logFailure(settleException);
        }
    }
}

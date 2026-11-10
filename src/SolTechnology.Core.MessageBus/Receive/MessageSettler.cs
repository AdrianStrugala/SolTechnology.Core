using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.MessageBus.Receive;

/// <summary>
/// Completes, abandons, or dead-letters a received message and logs the decision under the receiver's category.
/// A failing message is dead-lettered with reason <c>HandlerFailedMaxDelivery</c> once its delivery count reaches
/// <paramref name="maxDeliveryCount"/>. Settlement ignores <see cref="ProcessMessageEventArgs.CancellationToken"/>,
/// which the processor cancels as soon as shutdown starts, so a message handled during shutdown is still settled.
/// A settlement failure is logged at Warning and never re-settles the message; its lock expires and it is delivered
/// again.
/// </summary>
internal sealed class MessageSettler(ILogger logger, int maxDeliveryCount)
{
    private const string HandlerFailedMaxDelivery = "HandlerFailedMaxDelivery";

    public async Task<MessageSettlement> Complete(ProcessMessageEventArgs args, Type messageType)
    {
        var messageId = args.Message.MessageId;
        var completed = await TrySettle(
            () => args.CompleteMessageAsync(args.Message, CancellationToken.None),
            settleException => logger.LogWarning(settleException,
                "Failed to complete message [{MessageId}]; lock will expire naturally", messageId)).ConfigureAwait(false);
        if (completed)
        {
            logger.LogDebug(
                "Handled message [{MessageType}] [{MessageId}] (delivery [{DeliveryCount}])",
                messageType.Name, messageId, args.Message.DeliveryCount);
        }

        return new MessageSettlement(MessageSettlementAction.Completed, null, completed);
    }

    public async Task<MessageSettlement> DeadLetterUnreadable(ProcessMessageEventArgs args, Type messageType, DeserializedMessage message)
    {
        var messageId = args.Message.MessageId;
        string reason, description;
        switch (message.Failure)
        {
            case DeserializationFailure.EmptyBody:
                logger.LogError(
                    "Dead-lettering empty message [{MessageId}] for [{MessageType}]",
                    messageId, messageType.FullName);
                (reason, description) = ("EmptyBody", "Message body is empty.");
                break;
            case DeserializationFailure.InvalidJson:
                logger.LogError(message.Error,
                    "Dead-lettering message [{MessageId}] — deserialisation to [{MessageType}] failed",
                    messageId, messageType.FullName);
                (reason, description) = ("DeserializationFailed", message.Error!.Message);
                break;
            default:
                logger.LogError(
                    "Dead-lettering message [{MessageId}] — null payload after deserialisation to [{MessageType}]",
                    messageId, messageType.FullName);
                (reason, description) = ("NullPayload", "Deserialised payload is null.");
                break;
        }

        await DeadLetter(args, reason, description).ConfigureAwait(false);
        return new MessageSettlement(MessageSettlementAction.DeadLettered, reason, Settled: true);
    }

    public async Task<MessageSettlement> SettleFailure(ProcessMessageEventArgs args, Type messageType, Exception exception)
    {
        var messageId = args.Message.MessageId;
        var deliveryCount = args.Message.DeliveryCount;
        if (deliveryCount >= maxDeliveryCount)
        {
            logger.LogError(exception,
                "Handler failure for [{MessageType}] [{MessageId}] (delivery [{DeliveryCount}]) — dead-lettering on the last attempt",
                messageType.Name, messageId, deliveryCount);
            var deadLettered = await TrySettle(
                () => DeadLetter(args, HandlerFailedMaxDelivery, exception.Message),
                settleException => logger.LogWarning(settleException,
                    "Failed to dead-letter message [{MessageId}]; lock will expire naturally", messageId)).ConfigureAwait(false);
            return new MessageSettlement(MessageSettlementAction.DeadLettered, HandlerFailedMaxDelivery, deadLettered);
        }

        logger.LogError(exception,
            "Handler failure for [{MessageType}] [{MessageId}] (delivery [{DeliveryCount}]) — abandoning for retry",
            messageType.Name, messageId, deliveryCount);
        var abandoned = await TrySettle(
            () => args.AbandonMessageAsync(args.Message, cancellationToken: CancellationToken.None),
            settleException => logger.LogWarning(settleException,
                "Failed to abandon message [{MessageId}]; lock will expire naturally", messageId)).ConfigureAwait(false);
        return new MessageSettlement(MessageSettlementAction.Abandoned, null, abandoned);
    }

    private static Task DeadLetter(ProcessMessageEventArgs args, string reason, string description) =>
        args.DeadLetterMessageAsync(args.Message, reason, description, CancellationToken.None);

    private static async Task<bool> TrySettle(Func<Task> settle, Action<Exception> logFailure)
    {
        try
        {
            await settle().ConfigureAwait(false);
            return true;
        }
        catch (Exception settleException)
        {
            logFailure(settleException);
            return false;
        }
    }
}

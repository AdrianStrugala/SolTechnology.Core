using Azure.Messaging.ServiceBus;
using SolTechnology.Core.Logging.Chronicles;

namespace SolTechnology.Core.MessageBus.Receive;

/// <summary>
/// The Chronicle root of one received message (<c>Message {MessageType}</c>): identifiers and the
/// settlement only, never the body or the application properties.
/// </summary>
internal static class MessageChronicle
{
    public static ChronicleStep Begin(IChronicle chronicle, ProcessMessageEventArgs args, Type messageType)
        => chronicle.BeginRoot("Message " + messageType.Name)
            .Set("MessageId", args.Message.MessageId)
            .Set("DeliveryCount", args.Message.DeliveryCount)
            .Set("Entity", args.EntityPath);

    /// <summary>Records the settlement as <c>Settlement</c> and, when dead-lettered, <c>DeadLetterReason</c>.</summary>
    public static void Record(ChronicleStep root, MessageSettlement settlement)
    {
        if (!root.IsRecording)
        {
            return;
        }

        var completeFailed = settlement is { Action: MessageSettlementAction.Completed, Settled: false };
        root.Set("Settlement", completeFailed ? "CompleteFailed" : settlement.Action.ToString());
        if (settlement.DeadLetterReason is not null)
        {
            root.Set("DeadLetterReason", settlement.DeadLetterReason);
        }
    }
}

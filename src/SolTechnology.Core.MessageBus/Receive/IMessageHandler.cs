namespace SolTechnology.Core.MessageBus.Receive
{
    /// <summary>
    /// Handles messages of type <typeparamref name="TMessage"/> received from a queue or topic subscription. Resolved
    /// from a new DI scope for every message.
    /// </summary>
    /// <typeparam name="TMessage">The message type this handler receives.</typeparam>
    public interface IMessageHandler<in TMessage> where TMessage : IMessage
    {
        /// <summary>
        /// Processes one message. Returning completes the message; throwing abandons it for redelivery, or dead-letters it
        /// on delivery <c>MessageBusConfiguration.MaxDeliveryCount</c>. Delivery is at-least-once, so handlers should be idempotent.
        /// </summary>
        Task Handle(TMessage message, CancellationToken cancellationToken);
    }
}

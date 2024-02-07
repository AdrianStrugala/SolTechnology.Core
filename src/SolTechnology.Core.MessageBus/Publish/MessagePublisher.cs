using SolTechnology.Core.MessageBus.Broker;

namespace SolTechnology.Core.MessageBus.Publish;

public interface IMessagePublisher
{
    Task Publish(IMessage message);
}

public class MessagePublisher : IMessagePublisher
{
    private readonly IMessageBusBroker _messageBusBroker;

    public MessagePublisher(IMessageBusBroker messageBusBroker)
    {
        _messageBusBroker = messageBusBroker;
    }

    public async Task Publish(IMessage message)
    {
        var senders = _messageBusBroker.ResolveMessagePublisher(message.GetType().Name);

        foreach (var sender in senders)
        {
            await sender.Send(message);
        }
    }
}
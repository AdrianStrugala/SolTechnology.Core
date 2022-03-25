namespace SolTechnology.Core.MessageBus.Publish;

public interface IMessagePublisher
{
    Task Publish(IMessage message);
}
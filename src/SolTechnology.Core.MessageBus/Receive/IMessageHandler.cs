using SolTechnology.Core.MessageBus.Publish;

namespace SolTechnology.Core.MessageBus.Receive
{
    public interface IMessageHandler<in TMessage> where TMessage : IMessage
    {
        Task Handle(TMessage message, CancellationToken cancellationToken);
    }
}

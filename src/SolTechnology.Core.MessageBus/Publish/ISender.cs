namespace SolTechnology.Core.MessageBus.Publish
{
    public interface ISender
    {
        Task Send(IMessage message);

        Task Close();
    }
}

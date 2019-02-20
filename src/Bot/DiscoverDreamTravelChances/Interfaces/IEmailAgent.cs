namespace DreamTravel.Bot.DiscoverDreamTravelChances.Interfaces
{
    using Inftastructure.Email;

    public interface IEmailAgent
    {
        void Send(IEmail email);
    }
}
namespace DreamTravel.Bot.DiscoverDreamTravelChances.Interfaces
{
    using SendEmail;

    public interface IEmailAgent
    {
        void Send(DreamTravelChanceEmail email);
    }
}
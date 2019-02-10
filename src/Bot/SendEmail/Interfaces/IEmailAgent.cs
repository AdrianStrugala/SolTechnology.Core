namespace DreamTravel.Bot.SendEmail.Interfaces
{
    public interface IEmailAgent
    {
        void Send(DreamTravelChanceEmail email);
    }
}
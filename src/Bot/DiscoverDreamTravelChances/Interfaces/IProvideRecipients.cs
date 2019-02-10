namespace DreamTravel.Bot.DiscoverDreamTravelChances.Interfaces
{
    using System.Collections.Generic;
    using SendEmail;

    public interface IProvideRecipients
    {
        List<Recipient> Execute();
    }
}
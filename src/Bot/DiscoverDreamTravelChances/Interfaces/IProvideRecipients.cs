namespace DreamTravel.Bot.DiscoverDreamTravelChances.Interfaces
{
    using SendEmail;
    using System.Collections.Generic;

    public interface IProvideRecipients
    {
        List<User> Execute();
    }
}
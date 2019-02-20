namespace DreamTravel.Bot.DiscoverDreamTravelChances.Interfaces
{
    using SendEmail;
    using System.Collections.Generic;
    using Models;

    public interface IProvideRecipients
    {
        List<User> Execute();
    }
}
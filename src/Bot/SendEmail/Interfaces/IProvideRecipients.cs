namespace DreamTravel.Bot.SendEmail.Interfaces
{
    using System.Collections.Generic;

    public interface IProvideRecipients
    {
        List<Recipient> Execute();
    }
}
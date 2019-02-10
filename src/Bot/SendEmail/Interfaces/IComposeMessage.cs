namespace DreamTravel.Bot.SendEmail.Interfaces
{
    using System.Collections.Generic;
    using Scrap_AzairEu;

    public interface IComposeMessage
    {
        string Execute(List<Chance> chances);
        string ExecuteHtml(List<Chance> chances);
    }
}
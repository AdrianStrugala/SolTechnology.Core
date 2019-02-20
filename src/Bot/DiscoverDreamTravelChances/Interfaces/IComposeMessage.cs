namespace DreamTravel.Bot.DiscoverDreamTravelChances.Interfaces
{
    using System.Collections.Generic;
    using Models;

    public interface IComposeMessage
    {
        string Execute(List<Chance> chances);
        string ExecuteHtml(List<Chance> chances);
    }
}
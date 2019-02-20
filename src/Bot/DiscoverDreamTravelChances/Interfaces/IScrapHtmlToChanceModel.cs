namespace DreamTravel.Bot.DiscoverDreamTravelChances.Interfaces
{
    using System.Collections.Generic;
    using Models;

    public interface IScrapHtmlToChanceModel
    {
        List<Chance> Execute();
    }
}
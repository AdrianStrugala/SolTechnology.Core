namespace DreamTravel.Bot.DiscoverDreamTravelChances.Interfaces
{
    using System.Collections.Generic;
    using Models;
    using SharedModels;

    public interface IScrapHtmlToChanceModel
    {
        List<Chance> Execute();
    }
}
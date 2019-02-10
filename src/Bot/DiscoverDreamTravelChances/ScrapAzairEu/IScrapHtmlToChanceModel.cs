namespace DreamTravel.Bot.DiscoverDreamTravelChances.ScrapAzairEu
{
    using System.Collections.Generic;

    public interface IScrapHtmlToChanceModel
    {
        List<Chance> Execute();
    }
}
using System.Collections.Generic;

namespace DreamTravel.Bot.Scrap_AzairEu
{
    public interface IScrapHtmlToChanceModel
    {
        List<Chance> Execute();
    }
}
namespace DreamTravel.Bot.SendEmail
{
    using Scrap_AzairEu;
    using System.Collections.Generic;

    public interface IFilterChances
    {
        List<Chance> Execute(List<Chance> chances);
    }
}

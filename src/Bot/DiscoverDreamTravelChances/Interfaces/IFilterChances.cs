namespace DreamTravel.Bot.DiscoverDreamTravelChances.Interfaces
{
    using System.Collections.Generic;
    using Models;

    public interface IFilterChances
    {
        List<Chance> Execute(List<Chance> chances);
    }
}

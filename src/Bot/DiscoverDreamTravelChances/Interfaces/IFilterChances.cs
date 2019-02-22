namespace DreamTravel.Bot.DiscoverDreamTravelChances.Interfaces
{
    using System.Collections.Generic;
    using Models;
    using SharedModels;

    public interface IFilterChances
    {
        List<Chance> Execute(List<Chance> chances);
    }
}

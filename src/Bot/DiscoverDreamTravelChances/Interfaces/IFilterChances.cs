using DreamTravel.Domain.Flights;

namespace DreamTravel.Bot.DiscoverDreamTravelChances.Interfaces
{
    using System.Collections.Generic;
    using Models;

    public interface IFilterChances
    {
        List<Flight> Execute(List<Flight> chances);
    }
}

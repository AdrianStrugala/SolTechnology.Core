using DreamTravel.Domain.Flights;

namespace DreamTravel.Bot.DiscoverDreamTravelChances.Interfaces
{
    using System.Collections.Generic;
    using Models;

    public interface IComposeMessage
    {
        string Execute(List<Flight> chances);
        string ExecuteHtml(List<Flight> chances, string userName);
    }
}
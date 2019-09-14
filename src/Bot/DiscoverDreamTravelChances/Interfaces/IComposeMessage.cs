using DreamTravel.Domain.Flights;
using System.Collections.Generic;

namespace DreamTravel.Bot.DiscoverDreamTravelChances.Interfaces
{

    public interface IComposeMessage
    {
        string Execute(List<Flight> chances);
        string ExecuteHtml(List<Flight> chances, string userName);
    }
}
using System.Collections.Generic;
using DreamTravel.Bot.DiscoverIndividualChances.Models;

namespace DreamTravel.Features.GetFlightEmailOrders
{
    public interface IGetFlightEmailOrders
    {
        List<FlightEmailOrder> Execute();
    }
}
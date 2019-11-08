using System.Collections.Generic;
using DreamTravel.Domain.FlightEmailOrders;

namespace DreamTravel.Features.DreamFlight.GetFlightEmailOrdersForUser
{
    public interface IGetFlightEmailOrdersForUser
    {
        List<FlightEmailOrder> Execute();
    }
}
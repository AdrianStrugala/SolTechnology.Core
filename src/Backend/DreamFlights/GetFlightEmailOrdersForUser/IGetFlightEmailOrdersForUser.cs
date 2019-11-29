using System.Collections.Generic;
using DreamTravel.Domain.FlightEmailOrders;

namespace DreamTravel.DreamFlights.GetFlightEmailOrdersForUser
{
    public interface IGetFlightEmailOrdersForUser
    {
        List<FlightEmailOrder> Execute(int userId);
    }
}
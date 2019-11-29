using System.Collections.Generic;
using DreamTravel.Domain.FlightEmailOrders;

namespace DreamTravel.DreamFlights.GetFlightEmailData
{
    public interface IGetFlightEmailData
    {
        List<FlightEmailData> Execute();
    }
}
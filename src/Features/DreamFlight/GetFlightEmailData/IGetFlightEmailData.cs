using System.Collections.Generic;
using DreamTravel.Domain.FlightEmailOrders;

namespace DreamTravel.Features.DreamFlight.GetFlightEmailOrders
{
    public interface IGetFlightEmailData
    {
        List<FlightEmailData> Execute();
    }
}
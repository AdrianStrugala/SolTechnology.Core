using System.Collections.Generic;
using DreamTravel.Domain.FlightEmailOrders;

namespace DreamTravel.Features.GetFlightEmailOrders
{
    public interface IGetFlightEmailOrders
    {
        List<FlightEmailData> Execute();
    }
}
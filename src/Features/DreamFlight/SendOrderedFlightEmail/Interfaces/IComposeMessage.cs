using System.Collections.Generic;
using DreamTravel.Domain.FlightEmailOrders;
using DreamTravel.Domain.Flights;

namespace DreamTravel.Features.DreamFlight.SendOrderedFlightEmail.Interfaces
{
    public interface IComposeMessage
    {
        string Execute(List<Flight> flights, FlightEmailData flightEmailOrder);
    }
}
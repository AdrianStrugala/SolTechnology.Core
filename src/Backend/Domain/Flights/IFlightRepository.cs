using System.Collections.Generic;
using DreamTravel.Domain.Flights.GetFlights;

namespace DreamTravel.Domain.Flights
{
    public interface IFlightRepository
    {
        List<Flight> GetFlights(GetFlightsOrder order);
    }
}
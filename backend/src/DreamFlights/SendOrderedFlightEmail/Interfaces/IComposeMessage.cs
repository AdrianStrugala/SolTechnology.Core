using System.Collections.Generic;
using DreamTravel.DatabaseData.Query.GetSubscriptionDetailsByDay;
using DreamTravel.Domain.FlightEmailSubscriptions;
using DreamTravel.Domain.Flights;

namespace DreamTravel.DreamFlights.SendOrderedFlightEmail.Interfaces
{
    public interface IComposeMessage
    {
        string Execute(List<Flight> flights, FlightEmailData flightEmailData);
    }
}
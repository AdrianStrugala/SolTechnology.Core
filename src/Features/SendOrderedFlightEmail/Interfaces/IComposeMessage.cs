using System.Collections.Generic;
using DreamTravel.Bot.DiscoverIndividualChances.Models;
using DreamTravel.Domain.Flights;

namespace DreamTravel.Features.SendOrderedFlightEmail.Interfaces
{
    public interface IComposeMessage
    {
        string Execute(List<Flight> flights, FlightEmailOrder flightEmailOrder);
    }
}
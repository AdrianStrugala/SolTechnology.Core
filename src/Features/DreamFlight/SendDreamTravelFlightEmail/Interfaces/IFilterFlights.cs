using System.Collections.Generic;
using DreamTravel.Domain.Flights;

namespace DreamTravel.Features.DreamFlight.SendDreamTravelFlightEmail.Interfaces
{
    public interface IFilterFlights
    {
        List<Flight> Execute(List<Flight> chances);
    }
}

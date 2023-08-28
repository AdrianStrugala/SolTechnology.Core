using System.Collections.Generic;
using DreamTravel.Domain.Flights;

namespace DreamTravel.DreamFlights.SendDreamTravelFlightEmail.Interfaces
{
    public interface IFilterFlights
    {
        List<Flight> Execute(List<Flight> chances);
    }
}

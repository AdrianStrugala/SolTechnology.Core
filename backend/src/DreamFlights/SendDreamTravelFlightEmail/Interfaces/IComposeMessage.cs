using System.Collections.Generic;
using DreamTravel.Domain.Flights;

namespace DreamTravel.DreamFlights.SendDreamTravelFlightEmail.Interfaces
{

    public interface IComposeMessage
    {
        string ExecuteHtml(List<Flight> chances, string userName);
    }
}
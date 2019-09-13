using System.Collections.Generic;
using DreamTravel.Domain.Flights;

namespace DreamTravel.FlightData.Flights.GetFlights
{
    public class GetFlightsResult
    {
        public List<Flight> Flights { get; set; }

        public GetFlightsResult()
        {
            Flights = new List<Flight>();
        }
    }
}

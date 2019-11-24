using System.Collections.Generic;

namespace DreamTravel.Domain.Flights.GetFlights
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

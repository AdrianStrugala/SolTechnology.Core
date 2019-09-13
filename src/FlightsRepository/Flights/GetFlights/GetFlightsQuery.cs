using System.Collections.Generic;

namespace DreamTravel.FlightData.Flights.GetFlights
{
    public class GetFlightsQuery
    {
        private Dictionary<string, List<string>> Departures { get; set; }
        private Dictionary<string, List<string>> Arrivals { get; set; }
    }
}

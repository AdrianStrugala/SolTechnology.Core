using System;
using System.Collections.Generic;

namespace DreamTravel.FlightData.Flights.GetFlights
{
    public class GetFlightsQuery
    {
        public Dictionary<string, List<string>> Departures { get; set; }

        public Dictionary<string, List<string>> Arrivals { get; set; }

        public DateTime DepartureDate { get; set; }

        public DateTime ArrivalDate { get; set; }

        public int MinDaysToStay { get; set; }

        public int MaxDaysToStay { get; set; }

        public bool OneWay { get; set; }
    }
}

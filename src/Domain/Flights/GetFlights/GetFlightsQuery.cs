using System;
using System.Collections.Generic;

namespace DreamTravel.Domain.Flights.GetFlights
{
    public class GetFlightsQuery
    {
        public KeyValuePair<string, List<string>> Departures { get; set; }

        public KeyValuePair<string, List<string>> Arrivals { get; set; }

        public DateTime DepartureDate { get; set; }

        public DateTime ArrivalDate { get; set; }

        public int MinDaysToStay { get; set; }

        public int MaxDaysToStay { get; set; }

        public bool OneWay { get; set; }
    }
}

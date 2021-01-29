using System;
using System.Collections.Generic;

namespace DreamTravel.GeolocationData.AzairApi.GetFlights
{
    public class GetFlightsQuery
    {
        public KeyValuePair<string, List<string>> Departures { get; }

        public KeyValuePair<string, List<string>> Arrivals { get; }

        public DateTime DepartureDate { get; }

        public DateTime ArrivalDate { get; }

        public int MinDaysToStay { get; }

        public int MaxDaysToStay { get; }

        public bool OneWay { get; }


        public GetFlightsQuery(KeyValuePair<string, List<string>> departures, KeyValuePair<string, List<string>> arrivals, DateTime departureDate, DateTime arrivalDate, int minDaysToStay, int maxDaysToStay, bool oneWay = false)
        {
            Departures = departures;
            Arrivals = arrivals;
            DepartureDate = departureDate;
            ArrivalDate = arrivalDate;
            MinDaysToStay = minDaysToStay;
            MaxDaysToStay = maxDaysToStay;
            OneWay = oneWay;
        }
    }
}

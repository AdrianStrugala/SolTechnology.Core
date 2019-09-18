using System;

namespace DreamTravel.Bot.DiscoverIndividualChances.Models
{
    public class FlightEmailOrder
    {
        public int UserId { get; set; }

        public string UserName { get; set; }

        public string From { get; set; }

        public string To { get; set; }

        public DateTime DepartureDate { get; set; }

        public DateTime ArrivalDate { get; set; }

        public int MinDaysToStay { get; set; }

        public int MaxDaysToStay { get; set; }

        public bool OneWay { get; set; }

        public string Email { get; set; }

        public string Currency { get; set; }
    }
}

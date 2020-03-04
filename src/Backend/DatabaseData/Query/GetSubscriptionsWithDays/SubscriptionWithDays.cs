using System;

namespace DreamTravel.DatabaseData.Query.GetSubscriptionsWithDays
{
    public class SubscriptionWithDays
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string From { get; set; }

        public string To { get; set; }

        public DateTime DepartureDate { get; set; }

        public DateTime ArrivalDate { get; set; }

        public int MinDaysOfStay { get; set; }

        public int MaxDaysOfStay { get; set; }

        public bool OneWay { get; set; }

        public bool Monday { get; set; }
        public bool Tuesday { get; set; }
        public bool Wednesday { get; set; }
        public bool Thursday { get; set; }
        public bool Friday { get; set; }
        public bool Saturday { get; set; }
        public bool Sunday { get; set; }
    }
}

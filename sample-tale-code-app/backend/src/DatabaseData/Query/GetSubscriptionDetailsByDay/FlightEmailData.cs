﻿using System;

namespace DreamTravel.DatabaseData.Query.GetSubscriptionDetailsByDay
{
    public class FlightEmailData
    {
        public Guid UserId { get; set; }

        public string UserName { get; set; }

        public string From { get; set; }

        public string To { get; set; }

        public DateTime DepartureDate { get; set; }

        public DateTime ArrivalDate { get; set; }

        public int MinDaysOfStay { get; set; }

        public int MaxDaysOfStay { get; set; }

        public bool OneWay { get; set; }

        public string Email { get; set; }

        public string Currency { get; set; }
    }
}
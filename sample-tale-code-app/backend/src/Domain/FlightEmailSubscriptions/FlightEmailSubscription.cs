﻿using System;

namespace DreamTravel.Domain.FlightEmailSubscriptions
{
    public class FlightEmailSubscription
    {
        public int Id { get; set; }

        public Guid UserId { get; set; }

        public string From { get; set; }

        public string To { get; set; }

        public DateTime DepartureDate { get; set; }

        public DateTime ArrivalDate { get; set; }

        public int MinDaysOfStay { get; set; }

        public int MaxDaysOfStay { get; set; }

        public bool OneWay { get; set; }
    }
}
﻿using System;
using System.Collections.Generic;
using DreamTravel.DatabaseData.Query.GetSubscriptionDetailsByDay;

namespace DreamTravel.DreamFlights.GetTodaysFlightEmailData
{
    public class GetTodaysFlightEmailDataHandler : IGetTodaysFlightEmailData
    {
        private readonly IGetSubscriptionDetailsByDay _getSubscriptionDetailsByDay;

        public GetTodaysFlightEmailDataHandler(IGetSubscriptionDetailsByDay getSubscriptionDetailsByDay)
        {
            _getSubscriptionDetailsByDay = getSubscriptionDetailsByDay;
        }

        public List<FlightEmailData> Handle()
        {
            return _getSubscriptionDetailsByDay.Execute(DateTime.UtcNow.DayOfWeek.ToString());
        }
    }
}
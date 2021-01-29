using System;
using System.Collections.Generic;
using DreamTravel.DatabaseData.Query.GetSubscriptionDetailsByDay;

namespace DreamTravel.DreamFlights.GetTodaysFlightEmailData
{
    public class GetTodaysTodaysFlightEmailDataHandler : IGetTodaysFlightEmailData
    {
        private readonly IGetSubscriptionDetailsByDay _getSubscriptionDetailsByDay;

        public GetTodaysTodaysFlightEmailDataHandler(IGetSubscriptionDetailsByDay getSubscriptionDetailsByDay)
        {
            _getSubscriptionDetailsByDay = getSubscriptionDetailsByDay;
        }

        public List<FlightEmailData> Handle()
        {
            return _getSubscriptionDetailsByDay.Execute(DateTime.UtcNow.DayOfWeek.ToString());
        }
    }
}
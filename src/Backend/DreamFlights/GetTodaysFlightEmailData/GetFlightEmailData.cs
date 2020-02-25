using System;
using System.Collections.Generic;
using DreamTravel.DatabaseData.Query.GetSubscriptionDetailsByDay;

namespace DreamTravel.DreamFlights.GetFlightEmailData
{
    public class GetFlightEmailData : IGetFlightEmailData
    {
        private readonly IGetSubscriptionDetailsByDay _getSubscriptionDetailsByDay;

        public GetFlightEmailData(IGetSubscriptionDetailsByDay getSubscriptionDetailsByDay)
        {
            _getSubscriptionDetailsByDay = getSubscriptionDetailsByDay;
        }

        public List<FlightEmailData> Handle()
        {
            return _getSubscriptionDetailsByDay.Execute(DateTime.UtcNow.DayOfWeek.ToString());
        }
    }
}
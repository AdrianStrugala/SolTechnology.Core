using System.Collections.Generic;
using DreamTravel.DatabaseData.Query.GetSubscriptionDetailsByDay;

namespace DreamTravel.DreamFlights.GetTodaysFlightEmailData
{
    public interface IGetTodaysFlightEmailData
    {
        List<FlightEmailData> Handle();
    }
}
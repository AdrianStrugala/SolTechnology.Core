using System.Collections.Generic;
using DreamTravel.DatabaseData.Query.GetSubscriptionDetailsByDay;
using DreamTravel.Domain.FlightEmailSubscriptions;

namespace DreamTravel.DreamFlights.GetFlightEmailData
{
    public interface IGetFlightEmailData
    {
        List<FlightEmailData> Handle();
    }
}
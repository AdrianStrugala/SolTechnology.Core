using System.Collections.Generic;
using DreamTravel.Domain.FlightEmailSubscriptions;

namespace DreamTravel.DreamFlights.GetFlightEmailSubscriptionsForUser
{
    public interface IGetFlightEmailSubscriptionsForUser
    {
        List<FlightEmailSubscription> Execute(int userId);
    }
}
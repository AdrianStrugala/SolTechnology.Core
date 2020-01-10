using System.Collections.Generic;

namespace DreamTravel.Domain.FlightEmailSubscriptions
{
    public interface IFlightEmailSubscriptionRepository
    {
        int Insert(FlightEmailSubscription flightEmailSubscription);

        List<FlightEmailData> GetByDay(string day);

        List<FlightEmailSubscription> GetByUserId(int userId);

        void Delete(int id);
    }
}
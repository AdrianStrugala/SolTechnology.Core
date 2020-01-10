using System.Collections.Generic;

namespace DreamTravel.Domain.FlightEmailSubscriptions
{
    public interface IFlightEmailSubscriptionRepository
    {
        void Insert(FlightEmailSubscription flightEmailSubscription);

        List<FlightEmailData> GetAll();

        List<FlightEmailSubscription> GetByUserId(int userId);

        void Delete(int id);
    }
}
using System.Collections.Generic;
using DreamTravel.Domain.FlightEmailSubscriptions;

namespace DreamTravel.DreamFlights.GetFlightEmailSubscriptionsForUser
{
    public class GetFlightEmailSubscriptionsForUser : IGetFlightEmailSubscriptionsForUser
    {
        private readonly IFlightEmailSubscriptionRepository _flightEmailSubscriptionRepository;

        public GetFlightEmailSubscriptionsForUser(IFlightEmailSubscriptionRepository flightEmailSubscriptionRepository)
        {
            _flightEmailSubscriptionRepository = flightEmailSubscriptionRepository;
        }

        public List<FlightEmailSubscription> Execute(int userId)
        {
            return _flightEmailSubscriptionRepository.GetByUserId(userId);
        }
    }
}
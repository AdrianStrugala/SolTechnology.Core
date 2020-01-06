using System.Collections.Generic;
using DreamTravel.Domain.FlightEmailOrders;

namespace DreamTravel.DreamFlights.GetFlightEmailOrdersForUser
{
    public class GetFlightEmailOrdersForUser : IGetFlightEmailOrdersForUser
    {
        private readonly IFlightEmailSubscriptionRepository _flightEmailSubscriptionRepository;

        public GetFlightEmailOrdersForUser(IFlightEmailSubscriptionRepository flightEmailSubscriptionRepository)
        {
            _flightEmailSubscriptionRepository = flightEmailSubscriptionRepository;
        }

        public List<FlightEmailSubscription> Execute(int userId)
        {
            return _flightEmailSubscriptionRepository.GetByUserId(userId);
        }
    }
}
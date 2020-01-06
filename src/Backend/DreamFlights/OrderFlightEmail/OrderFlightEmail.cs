using DreamTravel.Domain.FlightEmailOrders;

namespace DreamTravel.DreamFlights.OrderFlightEmail
{
    public class OrderFlightEmail : IOrderFlightEmail
    {
        private readonly IFlightEmailSubscriptionRepository _flightEmailSubscriptionRepository;

        public OrderFlightEmail(IFlightEmailSubscriptionRepository flightEmailSubscriptionRepository)
        {
            _flightEmailSubscriptionRepository = flightEmailSubscriptionRepository;
        }
        public void Execute(FlightEmailSubscription flightEmailSubscription)
        {
            _flightEmailSubscriptionRepository.Insert(flightEmailSubscription);
        }

    }
}

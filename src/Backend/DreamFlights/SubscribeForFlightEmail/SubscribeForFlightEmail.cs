using DreamTravel.Domain.FlightEmailSubscriptions;

namespace DreamTravel.DreamFlights.SubscribeForFlightEmail
{
    public class SubscribeForFlightEmail : ISubscribeForFlightEmail
    {
        private readonly IFlightEmailSubscriptionRepository _flightEmailSubscriptionRepository;

        public SubscribeForFlightEmail(IFlightEmailSubscriptionRepository flightEmailSubscriptionRepository)
        {
            _flightEmailSubscriptionRepository = flightEmailSubscriptionRepository;
        }
        public void Execute(SubscribeForFlightEmailsCommand command)
        {
            _flightEmailSubscriptionRepository.Insert(command.FlightEmailSubscription);
        }

    }
}

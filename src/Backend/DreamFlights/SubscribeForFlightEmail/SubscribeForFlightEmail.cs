using DreamTravel.Domain.FlightEmailSubscriptions;

namespace DreamTravel.DreamFlights.SubscribeForFlightEmail
{
    public class SubscribeForFlightEmail : ISubscribeForFlightEmail
    {
        private readonly IFlightEmailSubscriptionRepository _flightEmailSubscriptionRepository;
        private readonly ISubscriptionDaysRepository _subscriptionDaysRepository;

        public SubscribeForFlightEmail(
            IFlightEmailSubscriptionRepository flightEmailSubscriptionRepository,
            ISubscriptionDaysRepository subscriptionDaysRepository)
        {
            _flightEmailSubscriptionRepository = flightEmailSubscriptionRepository;
            _subscriptionDaysRepository = subscriptionDaysRepository;
        }
        public void Execute(SubscribeForFlightEmailsCommand command)
        {
            command.SubscriptionDays.FlightEmailSubscriptionId = _flightEmailSubscriptionRepository.Insert(command.FlightEmailSubscription);

            _subscriptionDaysRepository.Insert(command.SubscriptionDays);
        }

    }
}

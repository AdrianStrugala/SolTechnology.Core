using DreamTravel.Domain.FlightEmailSubscriptions;

namespace DreamTravel.DreamFlights.SubscribeForFlightEmail
{
    public class SubscribeForFlightEmailsCommand
    {
        public FlightEmailSubscription FlightEmailSubscription { get; }

        public SubscriptionDays SubscriptionDays { get; }

        public SubscribeForFlightEmailsCommand(FlightEmailSubscription flightEmailSubscription, SubscriptionDays subscriptionDays)
        {
            FlightEmailSubscription = flightEmailSubscription;
            SubscriptionDays = subscriptionDays;
        }
    }
}

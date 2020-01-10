using DreamTravel.Domain.FlightEmailSubscriptions;

namespace DreamTravel.DreamFlights.SubscribeForFlightEmail
{
    public class SubscribeForFlightEmailsCommand
    {
        public FlightEmailSubscription FlightEmailSubscription { get; }

        public Days Days { get; }

        public SubscribeForFlightEmailsCommand(FlightEmailSubscription flightEmailSubscription, Days days)
        {
            FlightEmailSubscription = flightEmailSubscription;
            Days = days;
        }
    }
}

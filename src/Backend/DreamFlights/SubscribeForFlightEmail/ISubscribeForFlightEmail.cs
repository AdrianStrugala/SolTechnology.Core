using DreamTravel.Domain.FlightEmailSubscriptions;

namespace DreamTravel.DreamFlights.SubscribeForFlightEmail
{
    public interface ISubscribeForFlightEmail
    {
        void Execute(FlightEmailSubscription flightEmailSubscription);
    }
}

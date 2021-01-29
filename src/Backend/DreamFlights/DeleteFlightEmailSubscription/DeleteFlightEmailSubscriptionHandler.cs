using DreamTravel.Domain.FlightEmailSubscriptions;

namespace DreamTravel.DreamFlights.DeleteFlightEmailSubscription
{
    public class DeleteFlightEmailSubscriptionHandler : IDeleteFlightEmailSubscription
    {
        private readonly IFlightEmailSubscriptionRepository _flightEmailSubscriptionRepository;

        public DeleteFlightEmailSubscriptionHandler(IFlightEmailSubscriptionRepository flightEmailSubscriptionRepository)
        {
            _flightEmailSubscriptionRepository = flightEmailSubscriptionRepository;
        }


        public void Handle(int id)
        {
            _flightEmailSubscriptionRepository.Delete(id);
        }
    }
}
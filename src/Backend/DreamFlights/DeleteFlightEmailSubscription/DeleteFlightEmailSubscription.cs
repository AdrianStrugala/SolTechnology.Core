using DreamTravel.Domain.FlightEmailSubscriptions;

namespace DreamTravel.DreamFlights.DeleteFlightEmailSubscription
{
    public class DeleteFlightEmailSubscription : IDeleteFlightEmailSubscription
    {
        private readonly IFlightEmailSubscriptionRepository _flightEmailSubscriptionRepository;

        public DeleteFlightEmailSubscription(IFlightEmailSubscriptionRepository flightEmailSubscriptionRepository)
        {
            _flightEmailSubscriptionRepository = flightEmailSubscriptionRepository;
        }


        public void Execute(int id)
        {
            _flightEmailSubscriptionRepository.Delete(id);
        }
    }
}
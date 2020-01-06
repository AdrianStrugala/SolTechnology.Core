using DreamTravel.Domain.FlightEmailOrders;

namespace DreamTravel.DreamFlights.DeleteFlightEmailOrder
{
    public class DeleteFlightEmailOrder : IDeleteFlightEmailOrder
    {
        private readonly IFlightEmailSubscriptionRepository _flightEmailSubscriptionRepository;

        public DeleteFlightEmailOrder(IFlightEmailSubscriptionRepository flightEmailSubscriptionRepository)
        {
            _flightEmailSubscriptionRepository = flightEmailSubscriptionRepository;
        }


        public void Execute(int id)
        {
            _flightEmailSubscriptionRepository.Delete(id);
        }
    }
}
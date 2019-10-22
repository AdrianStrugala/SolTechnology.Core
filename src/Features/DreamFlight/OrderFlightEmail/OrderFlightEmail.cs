using DreamTravel.DatabaseData;
using DreamTravel.Domain.FlightEmailOrders;

namespace DreamTravel.Features.DreamFlight.OrderFlightEmail
{
    public class OrderFlightEmail : IOrderFlightEmail
    {
        private readonly IFlightEmailOrderRepository _flightEmailOrderRepository;

        public OrderFlightEmail(IFlightEmailOrderRepository flightEmailOrderRepository)
        {
            _flightEmailOrderRepository = flightEmailOrderRepository;
        }
        public void Execute(FlightEmailOrder flightEmailOrder)
        {
            _flightEmailOrderRepository.Insert(flightEmailOrder);
        }
    }
}

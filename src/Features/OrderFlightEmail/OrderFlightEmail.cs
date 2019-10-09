using DreamTravel.Bot.DiscoverIndividualChances.Models;
using DreamTravel.DatabaseData;

namespace DreamTravel.Features.OrderFlightEmail
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

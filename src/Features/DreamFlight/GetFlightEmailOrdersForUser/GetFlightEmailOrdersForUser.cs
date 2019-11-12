using System.Collections.Generic;
using DreamTravel.DatabaseData;
using DreamTravel.Domain.FlightEmailOrders;

namespace DreamTravel.Features.DreamFlight.GetFlightEmailOrdersForUser
{
    public class GetFlightEmailOrdersForUser : IGetFlightEmailOrdersForUser
    {
        private readonly IFlightEmailOrderRepository _flightEmailOrderRepository;

        public GetFlightEmailOrdersForUser(IFlightEmailOrderRepository flightEmailOrderRepository)
        {
            _flightEmailOrderRepository = flightEmailOrderRepository;
        }

        public List<FlightEmailOrder> Execute(int userId)
        {
            return _flightEmailOrderRepository.GetByUserId(userId);
        }
    }
}
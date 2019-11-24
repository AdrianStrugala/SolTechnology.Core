using System.Collections.Generic;
using DreamTravel.Domain.FlightEmailOrders;

namespace DreamTravel.DreamFlights.GetFlightEmailOrdersForUser
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
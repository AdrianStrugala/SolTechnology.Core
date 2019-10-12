using System.Collections.Generic;
using DreamTravel.DatabaseData;
using DreamTravel.Domain.FlightEmailOrders;

namespace DreamTravel.Features.GetFlightEmailOrders
{
    public class GetFlightEmailOrders : IGetFlightEmailOrders
    {
        private readonly IFlightEmailOrderRepository _flightEmailOrderRepository;

        public GetFlightEmailOrders(IFlightEmailOrderRepository flightEmailOrderRepository)
        {
            _flightEmailOrderRepository = flightEmailOrderRepository;
        }

        public List<FlightEmailData> Execute()
        {
            return _flightEmailOrderRepository.GetAll();
        }
    }
}
using System.Collections.Generic;
using DreamTravel.DatabaseData;
using DreamTravel.Domain.FlightEmailOrders;

namespace DreamTravel.Features.DreamFlight.GetFlightEmailOrders
{
    public class GetFlightEmailData : IGetFlightEmailData
    {
        private readonly IFlightEmailOrderRepository _flightEmailOrderRepository;

        public GetFlightEmailData(IFlightEmailOrderRepository flightEmailOrderRepository)
        {
            _flightEmailOrderRepository = flightEmailOrderRepository;
        }

        public List<FlightEmailData> Execute()
        {
            return _flightEmailOrderRepository.GetAll();
        }
    }
}
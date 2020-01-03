using System.Collections.Generic;
using DreamTravel.Domain.FlightEmailOrders;

namespace DreamTravel.DreamFlights.GetFlightEmailData
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
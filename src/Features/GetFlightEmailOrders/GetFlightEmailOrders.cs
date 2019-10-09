using System.Collections.Generic;
using DreamTravel.Bot.DiscoverIndividualChances.Models;
using DreamTravel.DatabaseData.FlightEmailOrders;

namespace DreamTravel.Features.GetFlightEmailOrders
{
    public class GetFlightEmailOrders : IGetFlightEmailOrders
    {
        private readonly FlightEmailOrderRepository _flightEmailOrderRepository;

        public GetFlightEmailOrders(FlightEmailOrderRepository flightEmailOrderRepository)
        {
            _flightEmailOrderRepository = flightEmailOrderRepository;
        }

        public List<FlightEmailOrder> Execute()
        {
            return _flightEmailOrderRepository.GetAll();
        }
    }
}
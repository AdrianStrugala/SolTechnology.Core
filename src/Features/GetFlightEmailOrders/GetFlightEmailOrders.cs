using System.Collections.Generic;
using DreamTravel.Bot.DiscoverIndividualChances.Models;
using DreamTravel.DatabaseData;

namespace DreamTravel.Features.GetFlightEmailOrders
{
    public class GetFlightEmailOrders : IGetFlightEmailOrders
    {
        private readonly IFlightEmailOrderRepository _flightEmailOrderRepository;

        public GetFlightEmailOrders(IFlightEmailOrderRepository flightEmailOrderRepository)
        {
            _flightEmailOrderRepository = flightEmailOrderRepository;
        }

        public List<FlightEmailOrder> Execute()
        {
            return _flightEmailOrderRepository.GetAll();
        }
    }
}
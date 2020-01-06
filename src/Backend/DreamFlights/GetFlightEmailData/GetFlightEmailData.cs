using System.Collections.Generic;
using DreamTravel.Domain.FlightEmailOrders;

namespace DreamTravel.DreamFlights.GetFlightEmailData
{
    public class GetFlightEmailData : IGetFlightEmailData
    {
        private readonly IFlightEmailSubscriptionRepository _flightEmailSubscriptionRepository;

        public GetFlightEmailData(IFlightEmailSubscriptionRepository flightEmailSubscriptionRepository)
        {
            _flightEmailSubscriptionRepository = flightEmailSubscriptionRepository;
        }

        public List<FlightEmailData> Execute()
        {
            return _flightEmailSubscriptionRepository.GetAll();
        }
    }
}
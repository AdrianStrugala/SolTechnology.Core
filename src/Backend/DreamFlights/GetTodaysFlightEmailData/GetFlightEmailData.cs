using System;
using System.Collections.Generic;
using DreamTravel.Domain.FlightEmailSubscriptions;

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
            return _flightEmailSubscriptionRepository.GetByDay(DateTime.UtcNow.DayOfWeek.ToString());
        }
    }
}
using System;
using DreamTravel.DatabaseData;
using DreamTravel.Domain.Flights;
using DreamTravel.FlightProviderData;
using DreamTravel.FlightProviderData.Flights.GetFlights;

namespace DreamTravel.Bot.DiscoverDreamTravelChances
{
    using Infrastructure.Email;
    using Interfaces;
    using SendEmail;
    using System.Collections.Generic;
    using Models;

    public class DiscoverDreamTravelChances : IDiscoverDreamTravelChances
    {
        private readonly IComposeMessage _composeMessage;
        private readonly IUserRepository _userRepository;
        private readonly IFlightRepository _flightRepository;
        private readonly IFilterChances _filterChances;

        public DiscoverDreamTravelChances(IComposeMessage composeMessage, IUserRepository userRepository, IFlightRepository flightRepository, IFilterChances filterChances)
        {
            _composeMessage = composeMessage;
            _userRepository = userRepository;
            _flightRepository = flightRepository;
            _filterChances = filterChances;
        }

        public void Execute()
        {
            GetFlightsQuery getFlightsQuery = new GetFlightsQuery
            {
                ArrivalDate = DateTime.UtcNow.AddMonths(3),
                DepartureDate = DateTime.UtcNow,
                Departures = new KeyValuePair<string, List<string>>("Wroclaw", new List<string> {"WRO"}),
                Arrivals = new KeyValuePair<string, List<string>>("Anywhere", new List<string> {"XXX"}),
                MinDaysToStay = 2,
                MaxDaysToStay = 5
            };

            List<Flight> flights = _flightRepository.GetFlights(getFlightsQuery).Flights;

            flights = _filterChances.Execute(flights);

            var users = _userRepository.GetUsers();

            foreach (var user in users)
            {
                string message = _composeMessage.ExecuteHtml(flights, user.Name);
                EmailAgent.Send(new DreamTravelChanceEmail(message, user.Email));
            }
        }
    }
}

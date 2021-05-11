using System.Collections.Generic;
using DreamTravel.DatabaseData.Query.GetSubscriptionDetailsByDay;
using DreamTravel.Domain.Airports;
using DreamTravel.Domain.Flights;
using DreamTravel.DreamFlights.SendOrderedFlightEmail.Interfaces;
using DreamTravel.DreamFlights.SendOrderedFlightEmail.Models;
using DreamTravel.GeolocationData;
using DreamTravel.GeolocationData.AzairApi.GetFlights;
using DreamTravel.Infrastructure.Email;
using Microsoft.Extensions.Logging;

namespace DreamTravel.DreamFlights.SendOrderedFlightEmail
{
    public class SendOrderedFlightEmail : ISendOrderedFlightEmail
    {
        private readonly IAzairApiClient _azairApiClient;
        private readonly IAirportRepository _airportRepository;
        private readonly IComposeMessage _composeMessage;
        private readonly IDreamFlightsConfiguration _dreamFlightsConfiguration;
        private readonly ILogger<SendOrderedFlightEmail> _logger;

        public SendOrderedFlightEmail(
            IAzairApiClient azairApiClient,
            IAirportRepository airportRepository,
            IComposeMessage composeMessage,
            IDreamFlightsConfiguration dreamFlightsConfiguration,
            ILogger<SendOrderedFlightEmail> logger)
        {
            _azairApiClient = azairApiClient;
            _airportRepository = airportRepository;
            _composeMessage = composeMessage;
            _dreamFlightsConfiguration = dreamFlightsConfiguration;
            _logger = logger;
        }

        public void Handle(FlightEmailData flightEmailData)
        {
            if (_dreamFlightsConfiguration.SendEmails)
            {
                GetFlightsQuery getFlightsQuery = new GetFlightsQuery
                (
                    new KeyValuePair<string, List<string>>(flightEmailData.From,
                        _airportRepository.GetByPlace(flightEmailData.From).Codes),
                    new KeyValuePair<string, List<string>>(flightEmailData.To,
                        _airportRepository.GetByPlace(flightEmailData.To).Codes),
                    flightEmailData.DepartureDate,
                    flightEmailData.ArrivalDate,
                    flightEmailData.MinDaysOfStay,
                    flightEmailData.MaxDaysOfStay
                );

                List<Flight> flights = _azairApiClient.GetFlights(getFlightsQuery);

                string message = _composeMessage.Execute(flights, flightEmailData);
                EmailAgent.Send(new OrderedFlightEmail(
                    message,
                    flightEmailData.Email,
                    $"{flightEmailData.UserName} choose your flight to {flightEmailData.To}!"));
            }
            else
            {
                _logger.LogInformation($"{nameof(SendOrderedFlightEmail)} triggered, but environment configuration is set to not send emails");
            }
        }
    }
}
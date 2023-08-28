using System;
using System.Collections.Generic;
using DreamTravel.DatabaseData.Query.GetPreviewUsers;
using DreamTravel.Domain.Flights;
using DreamTravel.DreamFlights.SendDreamTravelFlightEmail.Interfaces;
using DreamTravel.DreamFlights.SendDreamTravelFlightEmail.Models;
using DreamTravel.GeolocationData;
using DreamTravel.GeolocationData.AzairApi.GetFlights;
using DreamTravel.Infrastructure.Email;
using Microsoft.Extensions.Logging;

namespace DreamTravel.DreamFlights.SendDreamTravelFlightEmail
{
    public class SendDreamTravelFlightEmailHandler : ISendDreamTravelFlightEmail
    {
        private readonly IComposeMessage _composeMessage;
        private readonly IGetPreviewUsers _getPreviewUsers;
        private readonly IAzairApiClient _azairApiClient;
        private readonly IFilterFlights _filterFlights;
        private readonly IDreamFlightsConfiguration _dreamFlightsConfiguration;
        private readonly ILogger<SendDreamTravelFlightEmailHandler> _logger;

        public SendDreamTravelFlightEmailHandler(
            IComposeMessage composeMessage,
            IGetPreviewUsers getPreviewUsers,
            IAzairApiClient azairApiClient,
            IFilterFlights filterFlights,
            IDreamFlightsConfiguration dreamFlightsConfiguration,
            ILogger<SendDreamTravelFlightEmailHandler> logger)
        {
            _composeMessage = composeMessage;
            _getPreviewUsers = getPreviewUsers;
            _azairApiClient = azairApiClient;
            _filterFlights = filterFlights;
            _dreamFlightsConfiguration = dreamFlightsConfiguration;
            _logger = logger;
        }

        public void Handle()
        {
            if (_dreamFlightsConfiguration.SendEmails)
            {
                GetFlightsQuery getFlightsQuery = new GetFlightsQuery
                (
                    new KeyValuePair<string, List<string>>("Wroclaw", new List<string> { "WRO" }),
                    new KeyValuePair<string, List<string>>("Anywhere", new List<string> { "XXX" }),
                    DateTime.UtcNow,
                    DateTime.UtcNow.AddMonths(3),
                    2,
                    5
                );

                List<Flight> flights = _azairApiClient.GetFlights(getFlightsQuery);
                flights = _filterFlights.Execute(flights);

                var users = _getPreviewUsers.Execute();

                foreach (var user in users)
                {
                    string message = _composeMessage.ExecuteHtml(flights, user.Name);
                    EmailAgent.Send(new DreamTravelChanceEmail(message, user.Email));
                }
            }
            else
            {
                _logger.LogInformation($"{nameof(SendDreamTravelFlightEmailHandler)} triggered, but environment configuration is set to not send emails");
            }
        }
    }
}

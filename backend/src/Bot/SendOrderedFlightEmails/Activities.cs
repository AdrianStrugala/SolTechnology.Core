using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.DatabaseData.Query.GetSubscriptionDetailsByDay;
using DreamTravel.Domain.FlightEmailSubscriptions;
using DreamTravel.DreamFlights.GetTodaysFlightEmailData;
using DreamTravel.DreamFlights.SendOrderedFlightEmail.Interfaces;
using Microsoft.Azure.WebJobs;

namespace DreamTravel.Bot.SendOrderedFlightEmails
{
    public class Activities
    {
        public const string GetFlightEmailOrdersFunctionName = "GetFlightEmailOrders";
        public const string SendOrderedFlightEmailFunctionName = "SendOrderedFlightEmail";

        private readonly IGetTodaysFlightEmailData _getTodaysFlightEmailData;
        private readonly ISendOrderedFlightEmail _sendOrderedFlightEmail;

        public Activities(IGetTodaysFlightEmailData getTodaysFlightEmailData, ISendOrderedFlightEmail sendOrderedFlightEmail)
        {
            _getTodaysFlightEmailData = getTodaysFlightEmailData;
            _sendOrderedFlightEmail = sendOrderedFlightEmail;
        }

        [FunctionName(GetFlightEmailOrdersFunctionName)]
        public async Task<List<FlightEmailData>> GetFlightEmailOrders(
            [ActivityTrigger] object input)
        {
            return _getTodaysFlightEmailData.Handle();
        }


        [FunctionName(SendOrderedFlightEmailFunctionName)]
        public async Task SendOrderedFlightEmail(
            [ActivityTrigger] FlightEmailData input)
        {
            _sendOrderedFlightEmail.Handle(input);
        }
    }
}

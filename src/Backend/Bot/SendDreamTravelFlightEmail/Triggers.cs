using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DreamTravel.DreamFlights.SendDreamTravelFlightEmail.Interfaces;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace DreamTravel.Bot.SendDreamTravelFlightEmail
{
    public class Triggers
    {
        private const string TimeTriggerFunctionName = nameof(Bot.SendDreamTravelFlightEmail) + "Time";
        private const string HttpTriggerFunctionName = nameof(Bot.SendDreamTravelFlightEmail) + "Http";
        public const string HttpTriggerFunctionRoute = nameof(Bot.SendDreamTravelFlightEmail) + "/Now";

        private readonly ISendDreamTravelFlightEmail _sendDreamTravelFlightEmail;

        public Triggers(ISendDreamTravelFlightEmail sendDreamTravelFlightEmail)
        {
            _sendDreamTravelFlightEmail = sendDreamTravelFlightEmail;
        }

        [FunctionName(TimeTriggerFunctionName)]
        public async Task TimerTrigger(
            [TimerTrigger("0 0 8 * * *")] TimerInfo timer,
            [OrchestrationClient] DurableOrchestrationClient orchestrationClient)
        {
            _sendDreamTravelFlightEmail.Execute();
        }

        [FunctionName(HttpTriggerFunctionName)]
        public async Task<HttpResponseMessage> HttpTrigger(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = HttpTriggerFunctionRoute)] HttpRequestMessage req,
            [OrchestrationClient] DurableOrchestrationClientBase client)
        {
            _sendDreamTravelFlightEmail.Execute();
            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}

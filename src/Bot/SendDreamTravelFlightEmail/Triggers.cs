using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AzureFunctions.Autofac;
using DreamTravel.Features.SendDreamTravelFlightEmail.Interfaces;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace DreamTravel.Bot.SendDreamTravelFlightEmail
{
    [DependencyInjectionConfig(typeof(Startup))]
    public static class Triggers
    {
        private const string TimeTriggerFunctionName = nameof(Bot.SendDreamTravelFlightEmail) + "Time";
        private const string HttpTriggerFunctionName = nameof(Bot.SendDreamTravelFlightEmail) + "Http";
        public const string HttpTriggerFunctionRoute = nameof(Bot.SendDreamTravelFlightEmail) + "/Now";

        [FunctionName(TimeTriggerFunctionName)]
        public static async Task TimerTrigger(
            [TimerTrigger("0 0 8 * * *")] TimerInfo timer,
            [OrchestrationClient] DurableOrchestrationClient orchestrationClient,
            [Inject] ISendDreamTravelFlightEmail sendDreamTravelFlightEmail)
        {
            sendDreamTravelFlightEmail.Execute();
        }

        [FunctionName(HttpTriggerFunctionName)]
        public static async Task<HttpResponseMessage> HttpTrigger(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = HttpTriggerFunctionRoute)] HttpRequestMessage req,
            [OrchestrationClient] DurableOrchestrationClientBase client,
            [Inject] ISendDreamTravelFlightEmail sendDreamTravelFlightEmail)
        {
            sendDreamTravelFlightEmail.Execute();
            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}

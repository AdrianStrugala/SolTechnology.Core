namespace DreamTravel.Bot.DiscoverDreamTravelChances
{
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using AzureFunctions.Autofac;
    using Interfaces;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;

    [DependencyInjectionConfig(typeof(Startup))]
    public static class Triggers
    {
        private const string TimeTriggerFunctionName = nameof(Bot.DiscoverDreamTravelChances) + "Time";
        private const string HttpTriggerFunctionName = nameof(Bot.DiscoverDreamTravelChances) + "Http";
        public const string HttpTriggerFunctionRoute = nameof(Bot.DiscoverDreamTravelChances) + "/Now";

        [FunctionName(TimeTriggerFunctionName)]
        public static async Task TimerTrigger(
            [TimerTrigger("0 0 8 * * *")] TimerInfo timer,
            [OrchestrationClient] DurableOrchestrationClient orchestrationClient,
            [Inject] IDiscoverDreamTravelChances discoverDreamTravelChances)
        {
            discoverDreamTravelChances.Execute();
        }

        [FunctionName(HttpTriggerFunctionName)]
        public static async Task<HttpResponseMessage> HttpTrigger(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = HttpTriggerFunctionRoute)] HttpRequestMessage req,
            [OrchestrationClient] DurableOrchestrationClientBase client,
            [Inject] IDiscoverDreamTravelChances discoverDreamTravelChances)
        {
            discoverDreamTravelChances.Execute();
            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}

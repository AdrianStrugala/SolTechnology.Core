namespace DreamTravel.Bot
{
    using AzureFunctions.Autofac;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using DiscoverDreamTravelChances.Interfaces;

    [DependencyInjectionConfig(typeof(Startup))]
    public static class Triggers
    {
        private const string TimeTriggerFunctionName = "DiscoverDreamTravelChances";
        private const string HttpTriggerFunctionName = "DiscoverDreamTravelChancesHttp";
        public const string HttpTriggerFunctionRoute = "DiscoverDreamTravelChances/Now";

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

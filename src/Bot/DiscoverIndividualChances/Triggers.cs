namespace DreamTravel.Bot.DiscoverIndividualChances
{
    using AzureFunctions.Autofac;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using System.Net;
    using System.Net.Http;
    using Interfaces;

    [DependencyInjectionConfig(typeof(Startup))]
    public static class Triggers
    {
        private const string TimeTriggerFunctionName = nameof(DiscoverIndividualChances) + "Time";
        private const string HttpTriggerFunctionName = nameof(DiscoverIndividualChances) + "Http";
        public const string HttpTriggerFunctionRoute = nameof(DiscoverIndividualChances) + "/Now";

        [FunctionName(TimeTriggerFunctionName)]
        public static void TimerTrigger(
            [TimerTrigger("0 0 17 * * *")] TimerInfo timer,
            [OrchestrationClient] DurableOrchestrationClient orchestrationClient,
            [Inject] IDiscoverIndividualChances discoverIndividualChances)
        {
            discoverIndividualChances.Execute();
        }

        [FunctionName(HttpTriggerFunctionName)]
        public static HttpResponseMessage HttpTrigger(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = HttpTriggerFunctionRoute)] HttpRequestMessage req,
            [OrchestrationClient] DurableOrchestrationClientBase client,
            [Inject] IDiscoverIndividualChances discoverIndividualChances)
        {
            discoverIndividualChances.Execute();
            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}

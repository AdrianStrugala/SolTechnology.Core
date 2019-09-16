namespace DreamTravel.Bot.DiscoverIndividualChances
{
    using Interfaces;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    public class Triggers
    {
        private readonly IDiscoverIndividualChances _discoverIndividualChances;
        private const string TimeTriggerFunctionName = nameof(DiscoverIndividualChances) + "Time";
        private const string HttpTriggerFunctionName = nameof(DiscoverIndividualChances) + "Http";
        public const string HttpTriggerFunctionRoute = nameof(DiscoverIndividualChances) + "/Now";

        public Triggers(IDiscoverIndividualChances discoverIndividualChances)
        {
            _discoverIndividualChances = discoverIndividualChances;
        }

        [FunctionName(TimeTriggerFunctionName)]
        public async Task TimerTrigger(
            [TimerTrigger("0 0 17 * * *")] TimerInfo timer,
            [OrchestrationClient] DurableOrchestrationClient orchestrationClient)
        {
            await _discoverIndividualChances.Execute();
        }

        [FunctionName(HttpTriggerFunctionName)]
        public async Task<HttpResponseMessage> HttpTrigger(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = HttpTriggerFunctionRoute)] HttpRequestMessage req,
            [OrchestrationClient] DurableOrchestrationClientBase client)
        {
            await _discoverIndividualChances.Execute();
            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}

﻿using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace DreamTravel.Bot.RunBenchmark
{
    public static class Triggers
    {
        private const string HttpTriggerFunctionName = "RunBenchmarkHttp";
        public const string HttpTriggerFunctionRoute = "RunBenchmark/Now";


        [FunctionName(HttpTriggerFunctionName)]
        public static async Task<HttpResponseMessage> HttpTrigger(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = HttpTriggerFunctionRoute)] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient client)
        {
            await client.StartNewAsync(Orchestrator.OrchestratorFunctionName, null);
            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}

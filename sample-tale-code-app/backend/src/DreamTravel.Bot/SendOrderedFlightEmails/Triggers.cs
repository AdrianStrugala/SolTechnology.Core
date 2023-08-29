// using System.Net;
// using System.Net.Http;
// using System.Threading.Tasks;
// using Microsoft.Azure.WebJobs;
// using Microsoft.Azure.WebJobs.Extensions.Http;
//
// namespace DreamTravel.Bot.SendOrderedFlightEmails
// {
//     public static class Triggers
//     {
//         private const string HttpTriggerFunctionName = "SendOrderedFlightEmailsHttp";
//         private const string TimeTriggerFunctionName = "SendOrderedFlightEmailsTime";
//         public const string HttpTriggerFunctionRoute = "SendOrderedFlightEmails/Now";
//
//
//         [FunctionName(HttpTriggerFunctionName)]
//         public static async Task<HttpResponseMessage> HttpTrigger(
//             [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = HttpTriggerFunctionRoute)] HttpRequestMessage req,
//             [OrchestrationClient] DurableOrchestrationClientBase client)
//         {
//             await client.StartNewAsync(Orchestrator.OrchestratorFunctionName, null);
//             return req.CreateResponse(HttpStatusCode.OK);
//         }
//
//         [FunctionName(TimeTriggerFunctionName)]
//         public static async Task TimerTrigger(
//             [TimerTrigger("0 0 6 * * *")] TimerInfo timer,
//             [OrchestrationClient] DurableOrchestrationClientBase client)
//         {
//             await client.StartNewAsync(Orchestrator.OrchestratorFunctionName, null);
//         }
//     }
// }

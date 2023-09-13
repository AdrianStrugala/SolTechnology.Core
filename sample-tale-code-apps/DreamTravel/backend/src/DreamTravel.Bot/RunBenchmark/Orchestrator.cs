using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace DreamTravel.Bot.RunBenchmark
{
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using System.Threading.Tasks;

    public static class Orchestrator
    {
        public const string OrchestratorFunctionName = nameof(RunBenchmark) + nameof(Orchestrate);

        [FunctionName(OrchestratorFunctionName)]
        public static async Task Orchestrate([OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            await context.CallActivityAsync(Activities.AntColonyBenchmarkFunctionName, null);

            await context.CallActivityAsync(Activities.GodBenchmarkFunctionName, null);

            await context.CallActivityAsync(Activities.ChatGPTNearestNeighbourFunctionName, null);

            await context.CallActivityAsync(Activities.GoogleBingHeldKarpFunctionName, null);
        }
    }
}

namespace DreamTravel.Bot.RunBenchmark
{
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using System.Threading.Tasks;

    public static class Orchestrator
    {
        public const string OrchestratorFunctionName = nameof(RunBenchmark) + nameof(Orchestrate);

        [FunctionName(OrchestratorFunctionName)]
        public static async Task Orchestrate([OrchestrationTrigger] DurableOrchestrationContextBase context, ILogger log)
        {
            await context.CallActivityAsync(nameof(Activities.AntColonyBenchmarkFunctionName), null);

            await context.CallActivityAsync(nameof(Activities.GodBenchmarkFunctionName), null);
        }
    }
}

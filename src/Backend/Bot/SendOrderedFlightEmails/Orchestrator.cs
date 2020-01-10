using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DreamTravel.Domain.FlightEmailSubscriptions;
using Microsoft.Azure.WebJobs;

namespace DreamTravel.Bot.SendOrderedFlightEmails
{
    public static class Orchestrator
    {
        public const string OrchestratorFunctionName = nameof(SendOrderedFlightEmails) + nameof(Orchestrate);

        [FunctionName(OrchestratorFunctionName)]
        public static async Task Orchestrate(
            [OrchestrationTrigger] DurableOrchestrationContext context)
        {
            List<FlightEmailData> flightEmailData = await context.CallActivityWithRetryAsync<List<FlightEmailData>>(Activities.GetFlightEmailOrdersFunctionName, Retry.Options, null);

            //Break between sending orders in equal time (in 12h)
            int twelveHoursInSec = 43200;
            int pollingInterval = twelveHoursInSec / flightEmailData.Count;

            for (int i = 0; i < flightEmailData.Count; i++)
            {
                await context.CallActivityWithRetryAsync(Activities.SendOrderedFlightEmailFunctionName, Retry.Options, flightEmailData[i]);

                if (i != flightEmailData.Count - 1)
                {
                    // Orchestration sleeps until this time.
                    var nextCheck = context.CurrentUtcDateTime.AddSeconds(pollingInterval);
                    await context.CreateTimer(nextCheck, CancellationToken.None);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DreamTravel.Bot.DiscoverIndividualChances.Models;
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
            List<FlightEmailOrder> flightEmailOrders = await context.CallActivityWithRetryAsync<List<FlightEmailOrder>>(Activities.GetFlightEmailOrdersFunctionName, Retry.Options, null);
            
            
            //Split orders by equal time (in 12h ex)

            //Send mails in foreach

            //End

//            int pollingInterval = GetPollingInterval();
//            DateTime expiryTime = GetExpiryTime();

            foreach (FlightEmailOrder flightEmailOrder in flightEmailOrders)
            {
                await context.CallActivityWithRetryAsync(Activities.SendOrderedFlightEmailFunctionName, Retry.Options, flightEmailOrder);

                // Orchestration sleeps until this time.
//                var nextCheck = context.CurrentUtcDateTime.AddSeconds(pollingInterval);
//                await context.CreateTimer(nextCheck, CancellationToken.None);
            }
        }
    }
}

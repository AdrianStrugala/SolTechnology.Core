using SolTechnology.Core.Journey.Workflow.ChainFramework;
using SolTechnology.Core.Journey.Workflow.Handlers; // For SampleOrderContext
using SolTechnology.Core.Journey.Workflow.Steps.Dtos; // For DTOs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SolTechnology.Core.Journey.Workflow.Steps
{
    public class RequestUserInputStep : IUserInteractionChainStep<SampleOrderContext, CustomerDetailsInput, CustomerDetailsOutput>
    {
        public string StepId => "RequestCustomerDetails"; // Changed for clarity

        public Dictionary<string, Type> GetRequiredInputSchema()
        {
            return new Dictionary<string, Type>
            {
                { nameof(CustomerDetailsInput.Name), typeof(string) },
                { nameof(CustomerDetailsInput.Address), typeof(string) }
            };
        }

        // Execute: Checks if data is already in context; if not, requests it by pausing.
        public Task<Result> Execute(SampleOrderContext context)
        {
            if (string.IsNullOrEmpty(context.CustomerDetails)) // Check if we already have the info
            {
                return Task.FromResult(Result.Paused(
                    reason: "Waiting for customer name and address.",
                    nextStepId: StepId, // Stay on this step
                    requiredInputSchema: GetRequiredInputSchema()));
            }
            // If CustomerDetails is already populated (e.g. by HandleUserInputAsync in a previous cycle that didn't fully complete, or pre-filled)
            // then this step might be considered complete or do further automatic processing.
            // For this example, if data exists, we assume HandleUserInputAsync did its job.
            return Task.FromResult(Result.Success());
        }

        // HandleUserInputAsync: Processes the actual input when provided by the user.
        public Task<Result> HandleUserInputAsync(SampleOrderContext context, CustomerDetailsInput userInput)
        {
            if (userInput == null)
            {
                return Task.FromResult(Result.Failure("User input cannot be null."));
            }
            if (string.IsNullOrWhiteSpace(userInput.Name) || string.IsNullOrWhiteSpace(userInput.Address))
            {
                return Task.FromResult(Result.Failure("Name and Address cannot be empty."));
            }

            context.CustomerDetails = $"Name: {userInput.Name}, Address: {userInput.Address}";
            // Optionally, produce some output for TStepOutput if this step had one.
            // For this example, the main output (CustomerDetails) is written to the shared context.
            // Let's assume CustomerDetailsOutput is not strictly needed for this example's TStepOutput.

            // After processing input, the step is considered complete.
            return Task.FromResult(Result.Success());
        }
    }
}

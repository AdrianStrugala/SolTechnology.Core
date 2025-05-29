using SolTechnology.Core.CQRS;
using SolTechnology.Core.Journey.Workflow.ChainFramework;
using SolTechnology.Core.Journey.Workflow.Steps.Dtos;
namespace DreamTravel.Flows.SampleOrderWorkflow.Steps;

public class RequestUserInputStep : InteractiveFlowStep<SampleOrderContext, CustomerDetailsInput>
{
    public string StepId => "RequestCustomerDetails"; // Changed for clarity
    
    public override Task<Result> ExecuteWithUserInput(SampleOrderContext context, CustomerDetailsInput userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput.Name) || string.IsNullOrWhiteSpace(userInput.Address))
        {
            return Task.FromResult(Result.Fail("Name and Address cannot be empty."));
        }

        context.CustomerDetails = $"Name: {userInput.Name}, Address: {userInput.Address}";
        // Optionally, produce some output for TStepOutput if this step had one.
        // For this example, the main output (CustomerDetails) is written to the shared context.
        // Let's assume CustomerDetailsOutput is not strictly needed for this example's TStepOutput.

        // After processing input, the step is considered complete.
        return Task.FromResult(Result.Success());
    }

        
}
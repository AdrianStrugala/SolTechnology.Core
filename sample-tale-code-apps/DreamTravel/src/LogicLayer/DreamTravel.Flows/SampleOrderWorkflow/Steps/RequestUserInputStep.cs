using SolTechnology.Core.CQRS;
using SolTechnology.Core.Story;
using SolTechnology.Core.Journey.Workflow.Steps.Dtos;

namespace DreamTravel.Flows.SampleOrderWorkflow.Chapters;

public class RequestUserInputChapter : InteractiveChapter<SampleOrderNarration, CustomerDetailsInput>
{
    public override string ChapterId => "RequestCustomerDetails";

    public override Task<Result> ReadWithInput(SampleOrderNarration narration, CustomerDetailsInput userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput.Name) || string.IsNullOrWhiteSpace(userInput.Address))
        {
            return Task.FromResult(Result.Fail("Name and Address cannot be empty."));
        }

        narration.CustomerDetails = new CustomerDetails
        {
            Address = userInput.Address,
            Name = userInput.Name
        };

        return Task.FromResult(Result.Success());
    }
}
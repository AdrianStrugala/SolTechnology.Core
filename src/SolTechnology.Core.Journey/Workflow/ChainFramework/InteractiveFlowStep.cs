using SolTechnology.Core.CQRS;
using SolTechnology.Core.Journey.Models;

namespace SolTechnology.Core.Journey.Workflow.ChainFramework
{
    public abstract class InteractiveFlowStep<TContext, TStepInput> : IFlowStep<TContext>
        where TContext : class
        where TStepInput : class, new()  // Input DTO for the step
    {
        /// <summary>
        /// Returns a dictionary describing the expected TStepInput properties and their types.
        /// This helps in generating the API response for UI steps or validating input.
        /// </summary>
        /// <returns>A dictionary where keys are property names and values are their types.</returns>
        public List<DataField> GetRequiredInputSchema() => typeof(TStepInput).ToDataFields();

        /// <summary>
        /// Processes the provided user input.
        /// Updates the context with the input or its processed form.
        /// Returns a Result indicating success (input accepted, step may be complete or ready for further auto-execution) or failure (input invalid).
        /// </summary>
        /// <param name="context">The current workflow context.</param>
        /// <param name="userInput">The user-provided input for this step.</param>
        /// <returns>A Result indicating the outcome of processing the user input.</returns>
        public abstract Task<Result> ExecuteWithUserInput(TContext context, TStepInput userInput);

        // Note on Execute(TContext context) from IChainStep<TContext>:
        // For an IUserInteractionChainStep, the Execute method would typically:
        // 1. Check if the required input (represented by TStepInput) has already been provided and processed 
        //    (e.g., by checking a flag or specific data within TContext that HandleUserInputAsync would have set).
        // 2. If input is NOT yet provided/processed:
        //    Return Result.Paused(
        //        reason: "Waiting for user input.", 
        //        nextStepId: /* typically the ID of the current step itself */, 
        //        requiredInputSchema: GetRequiredInputSchema()
        //    );
        // 3. If input IS present/processed:
        //    Perform any further automated processing based on the input.
        //    Update TContext with TStepOutput.
        //    Return Result.Success(); 
        //    Or, if further processing leads to an issue, Result.Failure("...");
        public Task<Result> Execute(TContext context)
        {
            throw new NotImplementedException();
        }
    }
}

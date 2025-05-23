namespace SolTechnology.Core.Journey.Workflow.ChainFramework
{
    /// <summary>
    /// Marker interface for automated steps in a chain.
    /// Automated steps typically perform operations without direct user input at the time of execution.
    /// Their Execute method performs the action and returns Success or Failure.
    /// </summary>
    /// <typeparam name="TContext">The context type shared by steps in the chain.</typeparam>
    public interface IAutomatedChainStep<TContext> : IChainStep<TContext>
        where TContext : class
    {
        // This interface currently has no additional members. 
        // It serves as a marker to differentiate automated steps from user interaction steps 
        // if needed for type checking, DI registration, or specific handling in the chain orchestrator.
        // If IChainStep<TContext> is sufficient as the universal base and IUserInteractionChainStep 
        // provides the necessary specialization, this marker might be optional depending on the framework's evolution.
    }
}

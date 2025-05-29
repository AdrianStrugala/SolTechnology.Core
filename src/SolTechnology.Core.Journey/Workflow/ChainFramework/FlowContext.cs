// For FlowStatus and ExecutedStepInfo

namespace SolTechnology.Core.Journey.Workflow.ChainFramework;

public abstract class FlowContext<TInput, TOutput>
    where TInput : new()
    where TOutput : new()
{
    public TInput Input { get; set; } = new();
    public TOutput Output { get; set; } = new();
        
}
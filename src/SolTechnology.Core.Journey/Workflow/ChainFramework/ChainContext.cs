using SolTechnology.Core.Journey.Workflow; // For FlowStatus and ExecutedStepInfo
using System.Collections.Generic;

namespace SolTechnology.Core.Journey.Workflow.ChainFramework
{
    public class ChainContext<TInput, TOutput>
        where TInput : new()
        where TOutput : new()
    {
        public TInput Input { get; set; } = new TInput();
        public TOutput Output { get; set; } = new TOutput();


        // Default constructor is sufficient due to new() constraints and initializers.
        // public ChainContext()
        // {
        //     Input = new TInput();
        //     Output = new TOutput();
        //     History = new List<ExecutedStepInfo>();
        // }
    }
}

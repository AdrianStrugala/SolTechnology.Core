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
        public string? CurrentStepId { get; set; }
        public FlowStatus Status { get; set; }
        public List<ExecutedStepInfo> History { get; set; } = new List<ExecutedStepInfo>();
        public string? ErrorMessage { get; set; }

        // Default constructor is sufficient due to new() constraints and initializers.
        // public ChainContext()
        // {
        //     Input = new TInput();
        //     Output = new TOutput();
        //     History = new List<ExecutedStepInfo>();
        // }
    }
}

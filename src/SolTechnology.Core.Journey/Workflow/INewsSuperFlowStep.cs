using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SolTechnology.Core.Journey.Workflow
{
    public interface INewsSuperFlowStep
    {
        string StepId { get; }
        Task<NewsSuperFlowStepResult> ExecuteAsync(NewsSuperFlowInstance flowInstance, Dictionary<string, object> stepInput);
        Dictionary<string, Type> GetRequiredInputs();
        Dictionary<string, Type> GetExpectedOutputs();
    }
}

using System;
using System.Collections.Generic;

namespace SolTechnology.Core.Journey.Workflow
{
    public class NewsSuperFlowInstance
    {
        public string InstanceId { get; set; }
        public string FlowName { get; set; }
        public string CurrentStepId { get; set; }
        public FlowStatus Status { get; set; }
        public Dictionary<string, object> FlowInput { get; set; }
        public Dictionary<string, object> CurrentStepOutput { get; set; }
        public List<ExecutedStepInfo> ExecutedStepsHistory { get; set; }

        public NewsSuperFlowInstance()
        {
            InstanceId = Guid.NewGuid().ToString();
            ExecutedStepsHistory = new List<ExecutedStepInfo>();
            FlowInput = new Dictionary<string, object>();
            CurrentStepOutput = new Dictionary<string, object>();
        }
    }
}

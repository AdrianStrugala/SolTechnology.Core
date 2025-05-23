using System;
using System.Collections.Generic;

namespace SolTechnology.Core.Journey.Workflow
{
    public class ExecutedStepInfo
    {
        public string StepId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public FlowStatus Status { get; set; }
        public Dictionary<string, object> InputData { get; set; }
        public Dictionary<string, object> OutputData { get; set; }
        public string ErrorMessage { get; set; }

        public ExecutedStepInfo()
        {
            InputData = new Dictionary<string, object>();
            OutputData = new Dictionary<string, object>();
        }
    }
}

using System.Collections.Generic;

namespace SolTechnology.Core.Journey.Workflow
{
    public class NewsSuperFlowStepResult
    {
        public FlowStatus NextStepStatus { get; set; }
        public string NextStepId { get; set; }
        public Dictionary<string, object> OutputData { get; set; }
        public string Message { get; set; }
        public bool IsSuccess { get; set; }

        public NewsSuperFlowStepResult()
        {
            OutputData = new Dictionary<string, object>();
        }
    }
}

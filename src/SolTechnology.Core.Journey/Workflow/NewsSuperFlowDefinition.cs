using System;
using System.Collections.Generic;

namespace SolTechnology.Core.Journey.Workflow
{
    public class NewsSuperFlowDefinition
    {
        public string FlowName { get; set; }
        public List<string> StepExecutionOrder { get; set; }
        public Dictionary<string, Type> FlowInputSchema { get; set; }

        public NewsSuperFlowDefinition()
        {
            StepExecutionOrder = new List<string>();
            FlowInputSchema = new Dictionary<string, Type>();
        }
    }
}

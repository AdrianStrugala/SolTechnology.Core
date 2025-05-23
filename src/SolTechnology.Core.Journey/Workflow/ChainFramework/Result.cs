using System;
using System.Collections.Generic;

namespace SolTechnology.Core.Journey.Workflow.ChainFramework
{
    public class Result
    {
        public bool IsSuccess { get; private set; }
        public bool IsPaused { get; private set; } // Indicates the flow needs to pause (e.g., for user input)
        public string? Error { get; private set; }
        public string? NextStepIdOverride { get; private set; } // Optional: if a step wants to dynamically suggest the next step ID
        public string? PausedReason { get; private set; } // e.g., "Waiting for user input for XYZ."
        public Dictionary<string, Type>? RequiredInputSchemaForPause { get; private set; } // Details for UI steps

        private Result() { }

        public static Result Success() => new Result { IsSuccess = true };
        public static Result Failure(string error) => new Result { IsSuccess = false, Error = error };
        public static Result Paused(string reason, string? nextStepId = null, Dictionary<string, Type>? requiredInputSchema = null) 
            => new Result 
            { 
                IsSuccess = true, // A pause is not a failure of the step itself
                IsPaused = true, 
                PausedReason = reason, 
                NextStepIdOverride = nextStepId, 
                RequiredInputSchemaForPause = requiredInputSchema 
            };
    }
}

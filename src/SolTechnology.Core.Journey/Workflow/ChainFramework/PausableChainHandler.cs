using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Journey.Workflow; // For FlowStatus and ExecutedStepInfo
using System;
using System.Linq;
using System.Threading.Tasks;
using SolTechnology.Core.Journey.Models;

namespace SolTechnology.Core.Journey.Workflow.ChainFramework
{
    public interface IJourneyHandler;

    public abstract class PausableChainHandler<TInput, TContext, TOutput> : IJourneyHandler
        where TInput : new()
        where TOutput : new()
        where TContext : ChainContext<TInput, TOutput>, new()
    {
        protected readonly IServiceProvider _serviceProvider;
        protected readonly ILogger<PausableChainHandler<TInput, TContext, TOutput>> _logger;

        protected PausableChainHandler(IServiceProvider serviceProvider, ILogger<PausableChainHandler<TInput, TContext, TOutput>> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected abstract Task HandleChainDefinition(TContext context);

        public async Task<Result> ExecuteHandler(TContext context, string? requestedStepId = null)
        {
            _logger.LogInformation(
                "Executing handler for context {ContextType}, current status: {Status}, current context step: {ContextCurrentStep}, requested step: {RequestedStepId}",
                typeof(TContext).Name,
                context.Status,
                context.CurrentStepId,
                requestedStepId);

            if (context.Status == FlowStatus.Failed || context.Status == FlowStatus.Completed)
            {
                if (string.IsNullOrEmpty(requestedStepId) || requestedStepId == context.CurrentStepId) // only return terminal if not trying to re-enter a specific step
                {
                    _logger.LogWarning("Handler execution attempted on a terminal context (Status: {Status}) without specific step request. ContextCurrentStep: {ContextCurrentStep}", context.Status, context.CurrentStepId);
                    return Result.Failure($"Context is already in a terminal state: {context.Status}");
                }
            }

            // If a specific step is requested, it implies we are resuming or jumping.
            // If context is WaitingForInput, and the requestedStepId matches CurrentStepId, it's a direct resume.
            if (!string.IsNullOrEmpty(requestedStepId))
            {
                context.CurrentStepId = requestedStepId; // Set the current step to the requested one for resumption
                if(context.Status != FlowStatus.WaitingForInput) // If not explicitly waiting, assume it's running
                {
                     context.Status = FlowStatus.Running;
                }
            }
            else if (context.Status == FlowStatus.NotStarted || string.IsNullOrEmpty(context.CurrentStepId))
            {
                context.Status = FlowStatus.Running; // Fresh start
            }
            // If context.Status is WaitingForInput and no requestedStepId, it means it's still waiting.
            // HandleChainDefinition will manage skipping or executing based on context.CurrentStepId.

            await HandleChainDefinition(context);

            _logger.LogInformation("Handler execution finished for context {ContextType}. Final status: {Status}, CurrentStepId: {CurrentStepId}, ErrorMessage: {ErrorMessage}",
                typeof(TContext).Name, context.Status, context.CurrentStepId, context.ErrorMessage);

            if (context.Status == FlowStatus.Completed) return Result.Success();
            if (context.Status == FlowStatus.Failed) return Result.Failure(context.ErrorMessage ?? "Flow failed due to an unspecified error during chain execution.");
            if (context.Status == FlowStatus.WaitingForInput)
            {
                var lastPausedStepInfo = context.History.LastOrDefault(h => h.Status == FlowStatus.WaitingForInput && h.StepId == context.CurrentStepId);
                return Result.Paused(
                    lastPausedStepInfo?.ErrorMessage ?? context.ErrorMessage ?? "Flow is waiting for input.",
                    context.CurrentStepId
                    // RequiredInputSchemaForPause could be retrieved if stored in ExecutedStepInfo or context
                );
            }
            
            // If HandleChainDefinition completes and no step explicitly sets a terminal or waiting status,
            // and the status is still "Running", it implies the chain completed successfully.
            if (context.Status == FlowStatus.Running)
            {
                _logger.LogInformation("Handler for context {ContextType} completed all defined steps successfully.", typeof(TContext).Name);
                context.Status = FlowStatus.Completed;
                context.CurrentStepId = null; // No next step
                return Result.Success();
            }

            // Fallback for any other unhandled state, though ideally steps should drive context to a clear state.
            _logger.LogWarning("Handler for context {ContextType} ended with an unexpected status: {Status}", typeof(TContext).Name, context.Status);
            return Result.Failure($"Flow ended with an unexpected status: {context.Status}. Error: {context.ErrorMessage}");
        }

        protected async Task<Result> InvokeNextAsync<TStep>(TContext context, bool isContinuation = false)
            where TStep : class, IChainStep<TContext>
        {
            var stepId = typeof(TStep).Name; // Simple way to get a StepId. Could be an attribute or static property.

            // If we are not continuing a paused step, and the current context step is set (meaning a step is active/paused)
            // AND this active/paused step is NOT the one we are trying to invoke now, then we should not proceed.
            // This effectively means we only run the step that matches context.CurrentStepId if the flow is paused.
            if (!isContinuation && context.Status == FlowStatus.WaitingForInput && context.CurrentStepId != stepId)
            {
                 _logger.LogInformation("Skipping step {StepId} as context is waiting for step {ContextCurrentStepId}.", stepId, context.CurrentStepId);
                return Result.Success(); // Not an error, just not this step's turn.
            }

            // If context is already failed, don't run subsequent steps.
            if (context.Status == FlowStatus.Failed)
            {
                _logger.LogWarning("Skipping step {StepId} as context is already in Failed state.", stepId);
                return Result.Failure(context.ErrorMessage ?? "Context in failed state.");
            }
            
            // If context is completed, and we are not specifically continuing (which should not happen for completed), skip.
            if (context.Status == FlowStatus.Completed && !isContinuation)
            {
                 _logger.LogInformation("Skipping step {StepId} as context is already Completed.", stepId);
                return Result.Success();
            }

            TStep? stepInstance;
            try
            {
                stepInstance = _serviceProvider.GetService<TStep>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve step {StepId} from IServiceProvider.", stepId);
                context.Status = FlowStatus.Failed;
                context.ErrorMessage = $"Critical error: Failed to resolve step {stepId}.";
                context.CurrentStepId = stepId; // Mark this as the failing step
                // Add history for resolution failure
                context.History.Add(new StepInfo { StepId = stepId, StartedAt = DateTime.UtcNow, FinishedAt = DateTime.UtcNow, Status = FlowStatus.Failed, ErrorMessage = context.ErrorMessage });
                return Result.Failure(context.ErrorMessage);
            }

            if (stepInstance == null)
            {
                _logger.LogError("Step {StepId} could not be resolved (returned null) from IServiceProvider.", stepId);
                context.Status = FlowStatus.Failed;
                context.ErrorMessage = $"Critical error: Step {stepId} resolved to null.";
                context.CurrentStepId = stepId;
                context.History.Add(new StepInfo { StepId = stepId, StartedAt = DateTime.UtcNow, FinishedAt = DateTime.UtcNow, Status = FlowStatus.Failed, ErrorMessage = context.ErrorMessage });
                return Result.Failure(context.ErrorMessage);
            }

            // context.CurrentStepId = stepId; // Old: Set current step before execution
            context.CurrentStepId = typeof(TStep).AssemblyQualifiedName; // Use AssemblyQualifiedName

            if(context.Status != FlowStatus.WaitingForInput) // If not resuming from wait, it's Running
            {
                 context.Status = FlowStatus.Running;
            }

            StepInfo? stepHistory = context.History.LastOrDefault(h => h.StepId == context.CurrentStepId && h.Status == FlowStatus.Running);
            if (stepHistory == null || (isContinuation && context.Status == FlowStatus.WaitingForInput) ) // If resuming or first time
            {
                stepHistory = new StepInfo { StepId = context.CurrentStepId, StartedAt = DateTime.UtcNow, Status = FlowStatus.Running };
                context.History.Add(stepHistory);
            }
            else // It's a re-run of a step that wasn't Running (e.g. previous failure but chain continues)
            {
                stepHistory.StepId = context.CurrentStepId; // Ensure StepId is current if re-running an old history entry
                stepHistory.StartedAt = DateTime.UtcNow;
                stepHistory.Status = FlowStatus.Running;
                stepHistory.FinishedAt = null;
                stepHistory.ErrorMessage = null;
                stepHistory.ProvidedData.Clear(); // Clear previous output if any
            }


            _logger.LogInformation("Invoking step {StepId} for context {ContextType}. IsContinuation: {IsContinuation}", context.CurrentStepId, typeof(TContext).Name, isContinuation);
            Result stepResult;

            try
            {
                stepResult = await stepInstance.Execute(context);
                stepHistory.FinishedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Step {StepId} execution threw an unhandled exception.", context.CurrentStepId);
                stepHistory.FinishedAt = DateTime.UtcNow;
                stepHistory.Status = FlowStatus.Failed;
                stepHistory.ErrorMessage = ex.Message;
                
                context.Status = FlowStatus.Failed;
                context.ErrorMessage = $"Step {context.CurrentStepId} threw an exception: {ex.Message}";
                return Result.Failure(context.ErrorMessage);
            }

            stepHistory.Status = stepResult.IsPaused ? FlowStatus.WaitingForInput : (stepResult.IsSuccess ? FlowStatus.Completed : FlowStatus.Failed);
            stepHistory.ErrorMessage = stepResult.Error ?? stepResult.PausedReason;
            // stepHistory.OutputData might be populated by the step itself within the context or returned, for now assuming context is updated by step.

            if (!stepResult.IsSuccess && !stepResult.IsPaused) // Explicit failure
            {
                _logger.LogError("Step {StepId} failed: {Error}", context.CurrentStepId, stepResult.Error);
                context.Status = FlowStatus.Failed;
                context.ErrorMessage = stepResult.Error;
                return stepResult;
            }

            if (stepResult.IsPaused)
            {
                _logger.LogInformation("Step {StepId} paused: {Reason}. Next step override: {NextStepOverride}", context.CurrentStepId, stepResult.PausedReason, stepResult.NextStepIdOverride);
                context.Status = FlowStatus.WaitingForInput;
                context.ErrorMessage = stepResult.PausedReason; 
                context.CurrentStepId = stepResult.NextStepIdOverride ?? context.CurrentStepId; // Use current step AQN if override is null

                // Store pause details in history entry's OutputData
                if (stepHistory.ProvidedData == null)
                {
                    stepHistory.ProvidedData = new Dictionary<string, object?>();
                }
                stepHistory.ProvidedData["PausedReason"] = stepResult.PausedReason;
                if (stepResult.RequiredInputSchemaForPause != null) // Avoid storing null
                {
                    stepHistory.ProvidedData["RequiredInputSchemaForPause"] = stepResult.RequiredInputSchemaForPause;
                }
                
                return stepResult;
            }

            // If successful and not paused:
            _logger.LogInformation("Step {StepId} completed successfully.", stepId);
            // context.CurrentStepId = null; // Cleared, ready for the next InvokeNextAsync in HandleChainDefinition
                                         // This should be handled by the HandleChainDefinition or ExecuteHandler logic to advance
            return stepResult; // Result.Success()
        }
    }
}

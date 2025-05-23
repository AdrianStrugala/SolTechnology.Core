using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.Journey.Workflow.Persistence;
using Microsoft.Extensions.Logging; // Added using statement

namespace SolTechnology.Core.Journey.Workflow
{
    public class NewsSuperFlowManager
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, NewsSuperFlowDefinition> _flowDefinitions;
        private readonly INewsSuperFlowInstanceRepository _instanceRepository;
        private readonly Dictionary<string, INewsSuperFlowStep> _availableSteps;
        private readonly ILogger<NewsSuperFlowManager> _logger; // Added logger

        public NewsSuperFlowManager(
            IServiceProvider serviceProvider,
            INewsSuperFlowInstanceRepository instanceRepository,
            ILogger<NewsSuperFlowManager> logger) // Added logger
        {
            _serviceProvider = serviceProvider;
            _instanceRepository = instanceRepository;
            _logger = logger; // Assign logger
            _flowDefinitions = new Dictionary<string, NewsSuperFlowDefinition>();
            _availableSteps = new Dictionary<string, INewsSuperFlowStep>();
        }

        public void RegisterFlowDefinition(NewsSuperFlowDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }
            _flowDefinitions[definition.FlowName] = definition;
        }

        public void RegisterStep(INewsSuperFlowStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }
            _availableSteps[step.StepId] = step;
        }

        public async Task<NewsSuperFlowInstance> StartFlowAsync(string flowName, Dictionary<string, object> initialInput)
        {
            _logger.LogInformation("Attempting to start flow {FlowName}.", flowName);
            if (!_flowDefinitions.TryGetValue(flowName, out var definition))
            {
                throw new KeyNotFoundException($"Flow definition with name [{flowName}] not found.");
            }

            // Basic input validation (can be expanded)
            foreach (var requiredInput in definition.FlowInputSchema)
            {
                if (!initialInput.ContainsKey(requiredInput.Key) || initialInput[requiredInput.Key].GetType() != requiredInput.Value)
                {
                    // throw new ArgumentException($"Initial input does not match the flow's input schema. Missing or incorrect type for key: {requiredInput.Key}");
                }
            }

            var instance = new NewsSuperFlowInstance
            {
                FlowName = flowName,
                FlowInput = initialInput,
                Status = FlowStatus.NotStarted
            };

            if (definition.StepExecutionOrder.Any())
            {
                instance.CurrentStepId = definition.StepExecutionOrder.First();
                instance.Status = FlowStatus.Running; // Set to Running as we have a first step
            }
            else
            {
                instance.Status = FlowStatus.Completed; // No steps, so flow is completed
                instance.CurrentStepId = null;
            }

            await _instanceRepository.SaveAsync(instance); // Use repository
            _logger.LogInformation("Successfully started flow {FlowName} with instance ID {InstanceId}.", flowName, instance.InstanceId);

            // Optionally, you could immediately process the first step if it doesn't require external input.
            // For now, we'll keep it simple and require a separate ProcessFlowInstanceAsync call.
            // if (instance.Status == FlowStatus.Running)
            // {
            //     await ProcessFlowInstanceAsync(instance.InstanceId, null);
            // }

            return instance;
        }

        public async Task<NewsSuperFlowInstance?> GetFlowInstanceAsync(string instanceId)
        {
            var instance = await _instanceRepository.GetByIdAsync(instanceId);
            if (instance == null)
            {
                _logger.LogWarning("Flow instance {InstanceId} not found.", instanceId);
            }
            else
            {
                _logger.LogInformation("Flow instance {InstanceId} found.", instanceId);
            }
            return instance;
        }

        public async Task<NewsSuperFlowStepResult> ProcessFlowInstanceAsync(string instanceId, Dictionary<string, object>? stepInput)
        {
            var instance = await GetFlowInstanceAsync(instanceId);
            if (instance == null)
            {
                return new NewsSuperFlowStepResult { IsSuccess = false, Message = $"Flow instance [{instanceId}] not found.", NextStepStatus = FlowStatus.Failed };
            }

            if (instance.Status == FlowStatus.Completed || instance.Status == FlowStatus.Failed || instance.Status == FlowStatus.Cancelled)
            {
                return new NewsSuperFlowStepResult { IsSuccess = false, Message = $"Flow instance [{instanceId}] is already in a terminal state: {instance.Status}.", NextStepStatus = instance.Status };
            }

            if (string.IsNullOrEmpty(instance.CurrentStepId))
            {
                 instance.Status = FlowStatus.Completed;
                await _instanceRepository.SaveAsync(instance); // Save state change
                return new NewsSuperFlowStepResult { IsSuccess = true, Message = "No current step to process. Flow considered completed.", NextStepStatus = FlowStatus.Completed };
            }

            if (!_flowDefinitions.TryGetValue(instance.FlowName, out var definition))
            {
                instance.Status = FlowStatus.Failed;
                instance.ExecutedStepsHistory.Add(new ExecutedStepInfo
                {
                    StepId = instance.CurrentStepId,
                    StartedAt = DateTime.UtcNow,
                    FinishedAt = DateTime.UtcNow,
                    Status = FlowStatus.Failed,
                    ErrorMessage = $"Flow definition [{instance.FlowName}] not found."
                });
                await _instanceRepository.SaveAsync(instance); // Use repository
                return new NewsSuperFlowStepResult { IsSuccess = false, Message = $"Flow definition [{instance.FlowName}] not found.", NextStepStatus = FlowStatus.Failed };
            }
            
            // Resolve the step instance
            // INewsSuperFlowStep currentStepInstance;
            // try
            // {
            //     // Assuming StepExecutionOrder stores fully qualified type names or can be mapped to Type
            //     // For this subtask, we use _availableSteps for simplicity
            //     if (!_availableSteps.TryGetValue(instance.CurrentStepId, out currentStepInstance))
            //     {
            //          throw new InvalidOperationException($"Step with ID [{instance.CurrentStepId}] not registered or found.");
            //     }
            // }
            // catch (Exception ex)
            // {
            //     instance.Status = FlowStatus.Failed;
            //     instance.ExecutedStepsHistory.Add(new ExecutedStepInfo { /* ... error details ... */ ErrorMessage = ex.Message});
            //     _activeInstances[instance.InstanceId] = instance;
            //     return new NewsSuperFlowStepResult { IsSuccess = false, Message = ex.Message, NextStepStatus = FlowStatus.Failed };
            // }

            // Updated step resolution using _serviceProvider and _availableSteps as a fallback/primary for now
            INewsSuperFlowStep? currentStepInstance = null;
            if (_availableSteps.TryGetValue(instance.CurrentStepId, out var stepFromMap))
            {
                currentStepInstance = stepFromMap;
            }
            else
            {
                // Attempt to resolve from IServiceProvider if not in _availableSteps
                // This requires steps to be registered with their StepId as part of their service registration,
                // or a more complex resolution mechanism. For now, we'll assume _availableSteps is populated.
                // var stepType = Type.GetType(instance.CurrentStepId); // This would require CurrentStepId to be a fully qualified type name
                // if (stepType != null)
                // {
                //    currentStepInstance = _serviceProvider.GetService(stepType) as INewsSuperFlowStep;
                // }

                // Fallback or error if step cannot be resolved
                if (currentStepInstance == null)
                {
                    var errorMessage = $"Step with ID [{instance.CurrentStepId}] not registered or resolvable.";
                    _logger.LogError(errorMessage + " InstanceId: {InstanceId}", instance.InstanceId);
                    instance.Status = FlowStatus.Failed;
                    // Ensure history reflects this critical failure if a step can't even be resolved.
                    var unresolvedStepHistory = instance.ExecutedStepsHistory.LastOrDefault(h => h.StepId == instance.CurrentStepId && h.Status == FlowStatus.Running);
                    if (unresolvedStepHistory != null)
                    {
                        unresolvedStepHistory.Status = FlowStatus.Failed;
                        unresolvedStepHistory.FinishedAt = DateTime.UtcNow;
                        unresolvedStepHistory.ErrorMessage = errorMessage;
                    }
                    else // Should not happen if we add history before execution attempt
                    {
                         instance.ExecutedStepsHistory.Add(new ExecutedStepInfo {
                            StepId = instance.CurrentStepId,
                            StartedAt = DateTime.UtcNow, // Or a more appropriate time
                            FinishedAt = DateTime.UtcNow,
                            Status = FlowStatus.Failed,
                            ErrorMessage = errorMessage
                        });
                    }
                    await _instanceRepository.SaveAsync(instance);
                    return new NewsSuperFlowStepResult { IsSuccess = false, Message = errorMessage, NextStepStatus = FlowStatus.Failed };
                }
            }

            // Add step to history before execution
            var executedStepInfo = new ExecutedStepInfo
            {
                StepId = currentStepInstance.StepId, // Use resolved step's ID
                StartedAt = DateTime.UtcNow,
                Status = FlowStatus.Running, // Mark as Running initially
                InputData = stepInput ?? new Dictionary<string, object>()
            };
            instance.ExecutedStepsHistory.Add(executedStepInfo);
            // Persist this initial "Running" state of the step
            // This might be too chatty for some persistence layers, consider if this save is critical.
            // For now, we'll save to ensure the step attempt is recorded.
            await _instanceRepository.SaveAsync(instance);


            NewsSuperFlowStepResult stepResult;
            try
            {
                _logger.LogInformation("Executing step {StepId} for flow instance {InstanceId}.", currentStepInstance.StepId, instance.InstanceId);

                Dictionary<string, object> actualStepInput = stepInput ?? new Dictionary<string, object>();
                var previousCompletedStep = instance.ExecutedStepsHistory
                    .Where(s => s.Status == FlowStatus.Completed && s.StepId != currentStepInstance.StepId) // Exclude self if retrying
                    .OrderByDescending(s => s.FinishedAt)
                    .FirstOrDefault();

                if (previousCompletedStep != null)
                {
                    foreach (var item in previousCompletedStep.OutputData)
                    {
                        if (!actualStepInput.ContainsKey(item.Key))
                        {
                            actualStepInput[item.Key] = item.Value;
                        }
                    }
                }
                else if (!instance.ExecutedStepsHistory.Any(s => s.StepId == currentStepInstance.StepId && s.Status == FlowStatus.Running && s.StartedAt < executedStepInfo.StartedAt)) // Not a retry of self
                {
                    // First step (or no prior completed steps), merge FlowInput
                    foreach (var item in instance.FlowInput)
                    {
                        if (!actualStepInput.ContainsKey(item.Key))
                        {
                            actualStepInput[item.Key] = item.Value;
                        }
                    }
                }
                
                executedStepInfo.InputData = actualStepInput; // Update with actual input used

                stepResult = await currentStepInstance.ExecuteAsync(instance, actualStepInput);

                // Update history for successful execution
                executedStepInfo.FinishedAt = DateTime.UtcNow;
                executedStepInfo.OutputData = stepResult.OutputData;
                executedStepInfo.Status = stepResult.NextStepStatus;
                executedStepInfo.ErrorMessage = stepResult.IsSuccess ? string.Empty : stepResult.Message;
                _logger.LogInformation("Step {StepId} for flow instance {InstanceId} completed with status {NextStatus}.", currentStepInstance.StepId, instance.InstanceId, stepResult.NextStepStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception executing step {StepId} for flow instance {InstanceId}.", currentStepInstance.StepId, instance.InstanceId);
                
                // Update history for failure
                executedStepInfo.FinishedAt = DateTime.UtcNow;
                executedStepInfo.Status = FlowStatus.Failed;
                executedStepInfo.ErrorMessage = ex.Message;
                
                instance.Status = FlowStatus.Failed; // Mark overall flow as failed
                // CurrentStepId might be set to null later, or based on retry logic not yet implemented
                
                await _instanceRepository.SaveAsync(instance); // Persist failure state
                return new NewsSuperFlowStepResult { IsSuccess = false, Message = $"Error executing step {currentStepInstance.StepId}: {ex.Message}", NextStepStatus = FlowStatus.Failed };
            }

            // Note: executedStepInfo is already part of instance.ExecutedStepsHistory by reference.
            // So, modifications to executedStepInfo are already reflected in the list.

            instance.Status = stepResult.NextStepStatus;
            instance.CurrentStepOutput = stepResult.OutputData;

            if (stepResult.IsSuccess)
            {
                if (stepResult.NextStepStatus == FlowStatus.Completed || stepResult.NextStepStatus == FlowStatus.Running) // Step completed successfully
                {
                    int currentStepIndex = definition.StepExecutionOrder.IndexOf(instance.CurrentStepId);
                    if (currentStepIndex >= 0 && currentStepIndex < definition.StepExecutionOrder.Count - 1)
                    {
                        instance.CurrentStepId = definition.StepExecutionOrder[currentStepIndex + 1];
                        // If the step indicated it completed, but there's a next step, the flow is still Running.
                        // If the step itself decided the flow should pause (e.g. WaitingForInput), that status takes precedence.
                        if (instance.Status == FlowStatus.Completed) // A step might complete, but flow continues
                        {
                           instance.Status = FlowStatus.Running;
                        }
                    }
                    else // Last step completed
                    {
                        instance.CurrentStepId = null;
                        instance.Status = FlowStatus.Completed; // Flow is now fully completed
                    }
                }
                else // Step was successful but flow is not moving to Running/Completed (e.g. WaitingForInput, Suspended)
                {
                    instance.CurrentStepId = stepResult.NextStepId; 
                    // If the step wants to stay on itself (e.g. WaitingForInput), ensure CurrentStepId is not advanced by below logic accidentally
                    if (stepResult.NextStepId == currentStepInstance.StepId && instance.Status != FlowStatus.WaitingForInput && instance.Status != FlowStatus.Suspended)
                    {
                        // If it's a loop to self but not waiting, it's effectively Running the same step again.
                        // This might need more sophisticated handling for loops vs. simple retries.
                    }
                }
            }
            else // Step execution itself reported failure (IsSuccess = false in stepResult)
            {
                instance.Status = FlowStatus.Failed; 
                instance.CurrentStepId = null; 
                // The executedStepInfo status is already set to Failed by the stepResult.
            }
            // The following block for advancing CurrentStepId should only happen if the step *completed successfully* AND the flow is Running
            if (stepResult.IsSuccess && instance.Status == FlowStatus.Running)
            {
                 int currentStepIndex = definition.StepExecutionOrder.IndexOf(currentStepInstance.StepId);
                 if (currentStepIndex >= 0 && currentStepIndex < definition.StepExecutionOrder.Count - 1)
                 {
                     instance.CurrentStepId = definition.StepExecutionOrder[currentStepIndex + 1];
                 }
                 else // Last step completed successfully
                 {
                     instance.CurrentStepId = null;
                     instance.Status = FlowStatus.Completed; // Flow is now fully completed
                 }
            }
            else if (instance.Status == FlowStatus.Completed && string.IsNullOrEmpty(instance.CurrentStepId)) {
                 // This means the last step in definition just completed.
                  _logger.LogInformation("Flow {FlowName} instance {InstanceId} completed successfully.", instance.FlowName, instance.InstanceId);
            }


            await _instanceRepository.SaveAsync(instance);
            return stepResult;
        }
    }
}

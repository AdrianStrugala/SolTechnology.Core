using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Journey.Workflow; // For FlowStatus
using System;
using System.Collections.Generic; // For KeyNotFoundException
using System.Threading.Tasks;
using System.Text.Json; // Added for JsonElement and JsonSerializer
using System.Reflection; // Added for MethodInfo etc.
using System.Linq; // Added for Linq FirstOrDefault

namespace SolTechnology.Core.Journey.Workflow.ChainFramework
{
    public class JourneyManager
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IJourneyInstanceRepository _repository;
        private readonly ILogger<JourneyManager> _logger;

        public JourneyManager(
            IServiceProvider serviceProvider,
            IJourneyInstanceRepository repository,
            ILogger<JourneyManager> logger)
        {
            _serviceProvider = serviceProvider;
            _repository = repository;
            _logger = logger;
        }

        public async Task<JourneyInstance> StartJourneyAsync<THandler, TInput, TContext, TOutput>(TInput input)
            where THandler : PausableChainHandler<TInput, TContext, TOutput>
            where TInput : new()
            where TOutput : new()
            where TContext : ChainContext<TInput, TOutput>, new()
        {
            _logger.LogInformation("Starting new journey with handler {HandlerName}.", typeof(THandler).Name);

            THandler handler = _serviceProvider.GetRequiredService<THandler>();
            
            var context = new TContext { Input = input, Status = FlowStatus.NotStarted };
            var journeyId = Guid.NewGuid().ToString();
            
            var journeyInstance = new JourneyInstance(journeyId, typeof(THandler).AssemblyQualifiedName, context) 
            { 
                CurrentStatus = context.Status // Initial status from context
            };

            await _repository.SaveAsync(journeyInstance);
            _logger.LogInformation("Journey {JourneyId} instance created and saved. Initial status: {Status}", journeyId, context.Status);

            Result executionResult = await handler.ExecuteHandler(context);

            journeyInstance.ContextData = context;
            journeyInstance.LastUpdatedAt = DateTime.UtcNow;
            journeyInstance.CurrentStatus = context.Status; // Update status from context after execution
            await _repository.SaveAsync(journeyInstance);

            _logger.LogInformation(
                "Journey {JourneyId} started. Initial execution result: IsSuccess={IsSuccess}, IsPaused={IsPaused}, Error='{Error}'", 
                journeyId, 
                executionResult.IsSuccess, 
                executionResult.IsPaused, 
                executionResult.Error);

            return journeyInstance;
        }

        // Updated ResumeJourneyAsync to be non-generic in signature, but handle types internally
        public async Task<JourneyInstance> ResumeJourneyAsync(
            string journeyId, 
            object? userInput = null, // Accept userInput as object
            string? targetStepId = null) // targetStepId is useful if resuming at a specific point
        {
            _logger.LogInformation("Resuming journey {JourneyId}. TargetStepId: {TargetStepId}", journeyId, targetStepId);

            var journeyInstance = await _repository.GetByIdAsync(journeyId);

            if (journeyInstance == null)
            {
                _logger.LogError("Journey {JourneyId} not found.", journeyId);
                throw new KeyNotFoundException($"Journey {journeyId} not found.");
            }

            if (journeyInstance.ContextData == null)
            {
                _logger.LogError("ContextData for Journey {JourneyId} is null.", journeyId);
                throw new InvalidOperationException($"ContextData for Journey {journeyId} is null.");
            }
            
            if (string.IsNullOrEmpty(journeyInstance.FlowHandlerName))
            {
                 _logger.LogError("FlowHandlerName is null or empty for Journey {JourneyId}.", journeyId);
                 throw new InvalidOperationException($"FlowHandlerName is missing for journey {journeyId}.");
            }
            Type? handlerType = Type.GetType(journeyInstance.FlowHandlerName);
            if (handlerType == null)
            {
                _logger.LogError("Could not resolve handler type {HandlerName} for journey {JourneyId}.", journeyInstance.FlowHandlerName, journeyId);
                throw new InvalidOperationException($"Could not resolve handler type {journeyInstance.FlowHandlerName}.");
            }

            var baseHandlerType = handlerType.BaseType;
            if (baseHandlerType == null || !baseHandlerType.IsGenericType || baseHandlerType.GetGenericTypeDefinition() != typeof(PausableChainHandler<,,>))
            {
                 _logger.LogError("Handler type {HandlerName} is not a valid PausableChainHandler.", journeyInstance.FlowHandlerName);
                throw new InvalidOperationException($"Handler type {journeyInstance.FlowHandlerName} is not a valid PausableChainHandler.");
            }

            Type contextType = baseHandlerType.GetGenericArguments()[1]; // TContext type
            if (!contextType.IsAssignableFrom(journeyInstance.ContextData.GetType()))
            {
                 _logger.LogError("ContextData type mismatch for Journey {JourneyId}. Expected assignable from {ExpectedBase}, got {ActualType}", 
                    journeyId, contextType.FullName, journeyInstance.ContextData.GetType().FullName);
                throw new InvalidOperationException("ContextData type mismatch.");
            }
            var context = journeyInstance.ContextData; 

            var statusProperty = contextType.GetProperty("Status");
            var currentStatus = (FlowStatus)(statusProperty?.GetValue(context) ?? FlowStatus.Failed);

            if (currentStatus == FlowStatus.Completed || currentStatus == FlowStatus.Failed)
            {
                _logger.LogWarning("Attempt to resume journey {JourneyId} that is already in terminal state: {Status}", journeyId, currentStatus);
                return journeyInstance;
            }

            var handlerInstance = _serviceProvider.GetService(handlerType);
            if (handlerInstance == null)
            {
                _logger.LogError("Could not resolve handler instance for type {HandlerType}", handlerType.FullName);
                throw new InvalidOperationException($"Could not resolve handler instance for type {handlerType.FullName}");
            }
            
            var currentStepIdProperty = contextType.GetProperty("CurrentStepId");
            string? currentStepId = currentStepIdProperty?.GetValue(context) as string;

            if (userInput != null && !string.IsNullOrEmpty(currentStepId))
            {
                _logger.LogInformation("Processing user input for journey {JourneyId}, step {StepId}", journeyId, currentStepId);
                
                Type? currentStepType = Type.GetType(currentStepId); // Relies on CurrentStepId being AssemblyQualifiedName
                
                if (currentStepType != null && _serviceProvider.GetService(currentStepType) is var stepInstance && stepInstance != null)
                {
                    var userInteractionStepInterface = stepInstance.GetType().GetInterfaces().FirstOrDefault(i => 
                        i.IsGenericType && 
                        i.GetGenericTypeDefinition() == typeof(IUserInteractionChainStep<,,>));

                    if (userInteractionStepInterface != null)
                    {
                        Type stepInputType = userInteractionStepInterface.GetGenericArguments()[1]; 

                        object? typedUserInput;
                        if (userInput.GetType() == stepInputType) {
                            typedUserInput = userInput;
                        } else if (userInput is JsonElement jsonElement) { 
                            typedUserInput = jsonElement.Deserialize(stepInputType, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        }
                        else if (userInput is Dictionary<string, object> dict) { // Should be less common if controller sends JsonElement
                            var tempJson = JsonSerializer.Serialize(dict);
                            typedUserInput = JsonSerializer.Deserialize(tempJson, stepInputType, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        }
                         else {
                            _logger.LogError("User input type mismatch for step {StepId}. Expected {ExpectedType}, got {ActualType}", currentStepId, stepInputType.FullName, userInput.GetType().FullName);
                            throw new ArgumentException($"User input type mismatch for step {currentStepId}. Expected {stepInputType.FullName} but got {userInput.GetType().FullName}.");
                        }
                        
                        if (typedUserInput == null)
                        {
                            _logger.LogError("User input for step {StepId} deserialized to null.", currentStepId);
                            throw new ArgumentException($"User input for step {currentStepId} could not be processed.");
                        }

                        MethodInfo? handleMethod = userInteractionStepInterface.GetMethod("HandleUserInputAsync");
                        if (handleMethod == null) throw new InvalidOperationException("HandleUserInputAsync method not found on step.");

                        var taskResult = (Task<Result>?)handleMethod.Invoke(stepInstance, new[] { context, typedUserInput });
                        if (taskResult == null) throw new InvalidOperationException("Invoking HandleUserInputAsync returned null task.");
                        
                        Result inputHandlingResult = await taskResult;

                        if (!inputHandlingResult.IsSuccess)
                        {
                            _logger.LogWarning("Handling user input failed for journey {JourneyId}, step {StepId}. Error: {Error}", journeyId, currentStepId, inputHandlingResult.Error);
                            var errorProperty = contextType.GetProperty("ErrorMessage");
                            errorProperty?.SetValue(context, inputHandlingResult.Error);
                            
                            journeyInstance.ContextData = context;
                            journeyInstance.LastUpdatedAt = DateTime.UtcNow;
                            journeyInstance.CurrentStatus = (FlowStatus)(statusProperty?.GetValue(context) ?? FlowStatus.WaitingForInput); 
                            await _repository.SaveAsync(journeyInstance);
                            return journeyInstance; 
                        }
                         _logger.LogInformation("User input handled successfully for step {StepId}", currentStepId);
                    }
                } else {
                     _logger.LogWarning("User input provided for journey {JourneyId}, but current step {StepId} (Type: {CurrentStepTypeString}) could not be resolved or is not an interaction step.", 
                        journeyId, currentStepId, currentStepType?.FullName ?? "Not Resolved");
                }
            }
            
            // Dynamically invoke ExecuteHandler on the resolved handlerInstance
            MethodInfo? executeHandlerMethod = handlerType.GetMethod("ExecuteHandler");
            if (executeHandlerMethod == null) throw new InvalidOperationException("ExecuteHandler method not found on handler.");

            var executionTask = (Task<Result>?)executeHandlerMethod.Invoke(handlerInstance, new object?[] { context, targetStepId ?? currentStepId });
            if (executionTask == null) throw new InvalidOperationException("Invoking ExecuteHandler returned null task.");
            
            Result executionResult = await executionTask;
            
            journeyInstance.ContextData = context;
            journeyInstance.LastUpdatedAt = DateTime.UtcNow;
            journeyInstance.CurrentStatus = (FlowStatus)(statusProperty?.GetValue(context) ?? FlowStatus.Failed); 
            await _repository.SaveAsync(journeyInstance);

            _logger.LogInformation(
                    "Journey {JourneyId} resumed. Execution result: IsSuccess={IsSuccess}, IsPaused={IsPaused}, Error={Error}", 
                journeyId, executionResult.IsSuccess, executionResult.IsPaused, executionResult.Error);
            return journeyInstance;
        }
    }
}

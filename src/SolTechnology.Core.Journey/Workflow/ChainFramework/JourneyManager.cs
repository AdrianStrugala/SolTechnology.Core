using Microsoft.Extensions.Logging;
using System.Text.Json; 
using System.Reflection; 
using SolTechnology.Core.Journey.Models;
using SolTechnology.Core.Journey.Workflow.Persistence; 

namespace SolTechnology.Core.Journey.Workflow.ChainFramework
{
    public class JourneyManager(
        IServiceProvider serviceProvider,
        IJourneyInstanceRepository repository,
        ILogger<JourneyManager> logger)
    {
        public async Task<FlowInstance> StartFlow<THandler, TInput, TContext, TOutput>(TInput input)
            where THandler : PausableChainHandler<TInput, TContext, TOutput>
            where TInput : new()
            where TOutput : new()
            where TContext : FlowContext<TInput, TOutput>, new()
        {
            var flowName = typeof(THandler).AssemblyQualifiedName!;
            logger.LogInformation("Starting new flow [{HandlerName}].", flowName);
            
            var flowId = Guid.NewGuid().ToString();
            var context = new TContext
            {
                Input = input
            };
            
            var flowInstance = new FlowInstance(flowId, flowName, context);

            await repository.SaveAsync(flowInstance);
            
            logger.LogInformation("Flow {FlowId} instance created and saved. Initial status: {Status}",
                flowId, flowInstance.Status);


            return flowInstance;
        }

        // Updated ResumeJourneyAsync to be non-generic in signature, but handle types internally
        public async Task<FlowInstance> RunFlow(
            string journeyId,
            string? targetStepId,
            JsonElement? userInput = null
            ) 
        {
            logger.LogInformation("Resuming flow {JourneyId}. TargetStepId: {TargetStepId}", journeyId, targetStepId);

            var journeyInstance = await repository.FindById(journeyId);

            if (journeyInstance == null)
            {
                logger.LogError("Journey {JourneyId} not found.", journeyId);
                throw new KeyNotFoundException($"Journey {journeyId} not found.");
            }

            if (journeyInstance.Context == null)
            {
                logger.LogError("ContextData for Journey {JourneyId} is null.", journeyId);
                throw new InvalidOperationException($"ContextData for Journey {journeyId} is null.");
            }
            
            if (string.IsNullOrEmpty(journeyInstance.FlowHandlerName))
            {
                 logger.LogError("FlowHandlerName is null or empty for Journey {JourneyId}.", journeyId);
                 throw new InvalidOperationException($"FlowHandlerName is missing for journey {journeyId}.");
            }
            Type? handlerType = Type.GetType(journeyInstance.FlowHandlerName);
            if (handlerType == null)
            {
                logger.LogError("Could not resolve handler type {HandlerName} for journey {JourneyId}.", journeyInstance.FlowHandlerName, journeyId);
                throw new InvalidOperationException($"Could not resolve handler type {journeyInstance.FlowHandlerName}.");
            }

            var baseHandlerType = handlerType.BaseType;
            if (baseHandlerType == null || !baseHandlerType.IsGenericType || baseHandlerType.GetGenericTypeDefinition() != typeof(PausableChainHandler<,,>))
            {
                 logger.LogError("Handler type {HandlerName} is not a valid PausableChainHandler.", journeyInstance.FlowHandlerName);
                throw new InvalidOperationException($"Handler type {journeyInstance.FlowHandlerName} is not a valid PausableChainHandler.");
            }

            Type contextType = baseHandlerType.GetGenericArguments()[1]; // TContext type
            if (!contextType.IsInstanceOfType(journeyInstance.Context))
            {
                 logger.LogError("ContextData type mismatch for Journey {JourneyId}. Expected assignable from {ExpectedBase}, got {ActualType}", 
                    journeyId, contextType.FullName, journeyInstance.Context.GetType().FullName);
                throw new InvalidOperationException("ContextData type mismatch.");
            }
            var context = journeyInstance.Context;
            var currentStatus = journeyInstance.Status;

            if (currentStatus is FlowStatus.Completed or FlowStatus.Failed)
            {
                logger.LogWarning("Attempt to resume journey {JourneyId} that is already in terminal state: {Status}", journeyId, currentStatus);
                return journeyInstance;
            }

            var handlerInstance = serviceProvider.GetService(handlerType);
            if (handlerInstance == null)
            {
                logger.LogError("Could not resolve handler instance for type {HandlerType}", handlerType.FullName);
                throw new InvalidOperationException($"Could not resolve handler instance for type {handlerType.FullName}");
            }

            string? currentStepId = targetStepId ?? journeyInstance.CurrentStep?.StepId;
            
            // Dynamically invoke ExecuteHandler on the resolved handlerInstance
            MethodInfo? executeHandlerMethod = handlerType.GetMethod("ExecuteHandler");
            if (executeHandlerMethod == null) throw new InvalidOperationException("ExecuteHandler method not found on handler.");

            var executionTask = (Task?)executeHandlerMethod.Invoke(handlerInstance, [journeyInstance, context, currentStepId, userInput, CancellationToken.None]);
            if (executionTask == null) throw new InvalidOperationException("Invoking ExecuteHandler returned null task.");
            
            await executionTask;
            
            journeyInstance.LastUpdatedAt = DateTime.UtcNow;
            await repository.SaveAsync(journeyInstance);
            
            return journeyInstance;
        }
    }
}

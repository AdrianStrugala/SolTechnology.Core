using Microsoft.Extensions.Logging;
using System.Text.Json; 
using System.Reflection; 
using SolTechnology.Core.Flow.Models;
using SolTechnology.Core.Flow.Workflow.Persistence;

namespace SolTechnology.Core.Flow.Workflow.ChainFramework
{
    public class FlowManager(
        IServiceProvider serviceProvider,
        IFlowInstanceRepository repository,
        ILogger<FlowManager> logger)
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

        // Updated ResumeFlowAsync to be non-generic in signature, but handle types internally
        public async Task<FlowInstance> RunFlow(
            string flowId,
            string? targetStepId,
            JsonElement? userInput = null
            ) 
        {
            logger.LogInformation("Resuming flow {FlowId}. TargetStepId: {TargetStepId}", flowId, targetStepId);

            var flowInstance = await repository.FindById(flowId);

            if (flowInstance == null)
            {
                logger.LogError("Flow {FlowId} not found.", flowId);
                throw new KeyNotFoundException($"Flow {flowId} not found.");
            }

            if (flowInstance.Context == null)
            {
                logger.LogError("ContextData for Flow {FlowId} is null.", flowId);
                throw new InvalidOperationException($"ContextData for Flow {flowId} is null.");
            }
            
            if (string.IsNullOrEmpty(flowInstance.FlowHandlerName))
            {
                 logger.LogError("FlowHandlerName is null or empty for Flow {FlowId}.", flowId);
                 throw new InvalidOperationException($"FlowHandlerName is missing for flow {flowId}.");
            }
            Type? handlerType = Type.GetType(flowInstance.FlowHandlerName);
            if (handlerType == null)
            {
                logger.LogError("Could not resolve handler type {HandlerName} for flow {FlowId}.", flowInstance.FlowHandlerName, flowId);
                throw new InvalidOperationException($"Could not resolve handler type {flowInstance.FlowHandlerName}.");
            }

            var baseHandlerType = handlerType.BaseType;
            if (baseHandlerType == null || !baseHandlerType.IsGenericType || baseHandlerType.GetGenericTypeDefinition() != typeof(PausableChainHandler<,,>))
            {
                 logger.LogError("Handler type {HandlerName} is not a valid PausableChainHandler.", flowInstance.FlowHandlerName);
                throw new InvalidOperationException($"Handler type {flowInstance.FlowHandlerName} is not a valid PausableChainHandler.");
            }

            Type contextType = baseHandlerType.GetGenericArguments()[1]; // TContext type
            if (!contextType.IsInstanceOfType(flowInstance.Context))
            {
                 LoggerExtensions.LogError(logger, "ContextData type mismatch for Flow {FlowId}. Expected assignable from {ExpectedBase}, got {ActualType}",
                    flowId, contextType.FullName, flowInstance.Context.GetType().FullName);
                throw new InvalidOperationException("ContextData type mismatch.");
            }
            var context = flowInstance.Context;
            var currentStatus = flowInstance.Status;

            if (currentStatus is FlowStatus.Completed or FlowStatus.Failed)
            {
                logger.LogWarning("Attempt to resume flow {FlowId} that is already in terminal state: {Status}", flowId, currentStatus);
                return flowInstance;
            }

            var handlerInstance = serviceProvider.GetService(handlerType);
            if (handlerInstance == null)
            {
                logger.LogError("Could not resolve handler instance for type {HandlerType}", handlerType.FullName);
                throw new InvalidOperationException($"Could not resolve handler instance for type {handlerType.FullName}");
            }

            string? currentStepId = targetStepId ?? flowInstance.CurrentStep?.StepId;
            
            // Dynamically invoke ExecuteHandler on the resolved handlerInstance
            MethodInfo? executeHandlerMethod = handlerType.GetMethod("ExecuteHandler");
            if (executeHandlerMethod == null) throw new InvalidOperationException("ExecuteHandler method not found on handler.");

            var executionTask = (Task?)executeHandlerMethod.Invoke(handlerInstance, [flowInstance, context, currentStepId, userInput, CancellationToken.None]);
            if (executionTask == null) throw new InvalidOperationException("Invoking ExecuteHandler returned null task.");
            
            await executionTask;
            
            flowInstance.LastUpdatedAt = DateTime.UtcNow;
            await repository.SaveAsync(flowInstance);
            
            return flowInstance;
        }
    }
}

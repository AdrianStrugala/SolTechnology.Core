using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Flow.Models;
using SolTechnology.Core.Flow.Workflow;
using SolTechnology.Core.Flow.Workflow.ChainFramework;
using SolTechnology.Core.Flow.Workflow.Persistence;


namespace SolTechnology.Core.Flow.Controllers
{
    [ApiController]
    [Route("api/flow")]
    public abstract class FlowController(
        FlowManager flowManager,
        ILogger<FlowController> logger,
        IFlowInstanceRepository flowRepository,
        IEnumerable<IFlowHandler> flowHandlers)
        : ControllerBase
    {

        private readonly Dictionary<string, Type> _registeredHandlers = flowHandlers.ToDictionary(
            x => x.GetType().Name, y => y.GetType());

        [HttpPost("{flowName}/start")]
        public async Task<IActionResult> StartFlow(string flowName, [FromBody] JsonElement initialInputJson)
        {
            logger.LogInformation("Attempting to start flow with handler: {FlowHandlerName}", flowName);
            logger.LogInformation("Attempting to start flow with handler: {FlowHandlerName}", flowName);

            if (!_registeredHandlers.TryGetValue(flowName, out Type? handlerType))
            {
                return NotFound($"Flow '{flowName}' not registered.");
            }

            try
            {
                logger.LogInformation("Step 1: Validating handler type for {FlowName}", flowName);
                var baseHandlerType = handlerType.BaseType;
                if (baseHandlerType is not { IsGenericType: true } ||
                    baseHandlerType.GetGenericTypeDefinition() != typeof(PausableChainHandler<,,>))
                {
                    logger.LogError("Handler {FlowName} is not a valid PausableChainHandler", flowName);
                    return StatusCode(400, $"Flow '{flowName}' is not a valid PausableChainHandler.");
                }

                logger.LogInformation("Step 2: Getting input type");
                Type inputType = baseHandlerType.GetGenericArguments()[0];
                logger.LogInformation("Input type: {InputType}", inputType.FullName);

                object? typedInitialInput;
                try
                {
                    logger.LogInformation("Step 3: Deserializing input JSON");
                    typedInitialInput = initialInputJson.Deserialize(inputType,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                    logger.LogInformation("Input deserialized successfully");
                }
                catch (JsonException jsonEx)
                {
                    logger.LogError(jsonEx, "Failed to deserialize input");
                    return BadRequest(
                        $"Could not deserialize input for flow '{flowName}' to type {inputType.Name}. {jsonEx.Message}");
                }

                if (typedInitialInput == null)
                {
                    logger.LogError("Deserialized input is null");
                    return BadRequest($"Could not deserialize input for flow '{flowName}' to type {inputType.Name}.");
                }

                logger.LogInformation("Step 4: Creating generic StartFlow method");
                MethodInfo? startMethod = typeof(FlowManager).GetMethod(nameof(FlowManager.StartFlow))?
                    .MakeGenericMethod(handlerType, inputType, baseHandlerType.GetGenericArguments()[1],
                        baseHandlerType.GetGenericArguments()[2]);

                if (startMethod == null)
                {
                    logger.LogError("Could not create generic StartFlow method");
                    return StatusCode(400, "Could not make generic StartFlow method.");
                }
                logger.LogInformation("Generic method created successfully");

                logger.LogInformation("Step 5: Invoking StartFlow method");
                Task? task;
                try
                {
                    task = (Task?)startMethod.Invoke(flowManager, [typedInitialInput]);
                    logger.LogInformation("StartFlow invoked, task type: {TaskType}", task?.GetType().FullName ?? "null");
                }
                catch (Exception invokeEx)
                {
                    logger.LogError(invokeEx, "Failed to invoke StartFlow");
                    return StatusCode(500, $"Failed to invoke StartFlow: {invokeEx.Message}");
                }

                if (task == null)
                {
                    logger.LogError("StartFlow returned null task");
                    return StatusCode(400, "Could not invoke StartFlow.");
                }

                logger.LogInformation("Step 6: Awaiting task");
                try
                {
                    await task;
                    logger.LogInformation("Task completed successfully");
                }
                catch (Exception taskEx)
                {
                    logger.LogError(taskEx, "Task failed during StartFlow execution for {FlowHandlerName}.", flowName);
                    return StatusCode(500, $"An error occurred during flow execution: {taskEx.Message}");
                }

                logger.LogInformation("Step 7: Extracting result from task");
                var taskType = task.GetType();
                logger.LogInformation("Task type: {TaskType}, IsGenericType: {IsGeneric}", taskType.FullName, taskType.IsGenericType);

                if (!taskType.IsGenericType)
                {
                    logger.LogError("Task is not generic");
                    return StatusCode(500, "StartFlow did not return a generic Task<T>.");
                }

                var resultProperty = taskType.GetProperty("Result");
                if (resultProperty == null)
                {
                    logger.LogError("Could not find Result property on task");
                    return StatusCode(500, "Could not find Result property on Task.");
                }

                logger.LogInformation("Step 8: Getting Result value");
                object? resultValue;
                try
                {
                    resultValue = resultProperty.GetValue(task);
                    logger.LogInformation("Result value type: {ResultType}", resultValue?.GetType().FullName ?? "null");
                }
                catch (Exception resultEx)
                {
                    logger.LogError(resultEx, "Failed to get Result value");
                    return StatusCode(500, $"Failed to get result: {resultEx.Message}");
                }

                var flowInstance = resultValue as FlowInstance;
                if (flowInstance == null)
                {
                    logger.LogError("Result is not a FlowInstance, actual type: {ActualType}", resultValue?.GetType().FullName ?? "null");
                    return StatusCode(500, "Result was not a FlowInstance.");
                }

                logger.LogInformation("Step 9: Returning flow instance");
                return Ok(flowInstance);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error starting flow {FlowHandlerName}. Exception type: {ExceptionType}, StackTrace: {StackTrace}",
                    flowName, ex.GetType().FullName, ex.StackTrace);
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("{flowId}")]
        public async Task<IActionResult> RunFlow(
            string flowId,
            [FromBody] JsonElement? userInputJson,
            [FromQuery] string? stepId)
        {
            logger.LogInformation("Attempting to resume flow: {flowId}", flowId);

            JsonElement? userInput = userInputJson?.ValueKind == JsonValueKind.Undefined || userInputJson == null
                    ? null
                    : userInputJson;

                var flowInstance = await flowRepository.FindById(flowId);
                if (flowInstance == null)
                {
                    return BadRequest($"Flow {flowId} not found.");
                }

                Type? handlerType = Type.GetType(flowInstance.FlowHandlerName);

                if (handlerType == null || !_registeredHandlers.ContainsValue(handlerType))
                {
                    logger.LogError(
                        "Handler type {HandlerName} for flow {FlowId} is not registered or cannot be resolved.",
                        flowInstance.FlowHandlerName, flowId);
                    return StatusCode(400,
                        $"Handler type {flowInstance.FlowHandlerName} for flow {flowId} is not registered or cannot be resolved.");
                }

                var baseHandlerType = handlerType.BaseType;
                if (baseHandlerType == null || !baseHandlerType.IsGenericType ||
                    baseHandlerType.GetGenericTypeDefinition() != typeof(PausableChainHandler<,,>))
                {
                    return StatusCode(400,
                        $"Handler '{flowInstance.FlowHandlerName}' is not a valid PausableChainHandler.");
                }

                flowInstance = await flowManager.RunFlow(
                    flowId,
                    stepId ?? flowInstance.CurrentStep?.StepId,
                    userInput);

                return Ok(flowInstance);
        }

        [HttpGet("{flowId}")]
        public async Task<IActionResult> GetFlowState(string flowId)
        {
            logger.LogInformation("Attempting to get status for flow: {FlowId}", flowId);
            if (string.IsNullOrEmpty(flowId))
            {
                return BadRequest("Flow ID must be provided.");
            }

            var flowInstance = await flowRepository.FindById(flowId);
            return Ok(flowInstance);
        }
        
        [HttpGet("{flowId}/result")]
        public async Task<IActionResult> GetFlowResult(string flowId)
        {
            logger.LogInformation("Attempting to get result for flow: {FlowId}", flowId);
            if (string.IsNullOrEmpty(flowId))
            {
                return BadRequest("Flow ID must be provided.");
            }

            var flowInstance = await flowRepository.FindById(flowId);
            return Ok(flowInstance!.Context!.Output);
        }
    }
}

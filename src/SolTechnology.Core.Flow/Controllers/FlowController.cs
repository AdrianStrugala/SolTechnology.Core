using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Flow.Models;
using SolTechnology.Core.Flow.Workflow;
using SolTechnology.Core.Flow.Workflow.ChainFramework;
using SolTechnology.Core.Flow.Workflow.Persistence;

namespace SolTechnology.Core.Flow.Controllers;

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
        logger.LogInformation("Starting flow: {FlowName}", flowName);

        if (!TryGetHandlerType(flowName, out var handlerType) || handlerType == null)
        {
            return NotFound($"Flow '{flowName}' not registered.");
        }

        try
        {
            var validationResult = ValidateHandlerType(handlerType, flowName);
            if (validationResult != null)
            {
                return validationResult;
            }

            var baseHandlerType = handlerType.BaseType!;
            var inputType = baseHandlerType.GetGenericArguments()[0];

            var deserializationResult = DeserializeInput(initialInputJson, inputType, flowName);
            if (deserializationResult.Error != null)
            {
                return deserializationResult.Error;
            }

            var startMethod = CreateStartFlowMethod(handlerType, baseHandlerType);
            if (startMethod == null)
            {
                logger.LogError("Could not create generic StartFlow method for {FlowName}", flowName);
                return StatusCode(500, "Could not create generic StartFlow method.");
            }

            var flowInstance = await ExecuteStartFlow(startMethod, deserializationResult.Input!, flowName);
            if (flowInstance.Error != null)
            {
                return flowInstance.Error;
            }

            return Ok(flowInstance.Instance);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting flow {FlowName}", flowName);
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }

    [HttpPost("{flowId}")]
    public async Task<IActionResult> RunFlow(
        string flowId,
        [FromBody] JsonElement? userInputJson,
        [FromQuery] string? stepId)
    {
        logger.LogInformation("Resuming flow: {FlowId}", flowId);

        var userInput = NormalizeUserInput(userInputJson);
        var flowInstance = await flowRepository.FindById(flowId);

        if (flowInstance == null)
        {
            return NotFound($"Flow {flowId} not found.");
        }

        var handlerValidation = ValidateFlowHandler(flowInstance, flowId);
        if (handlerValidation != null)
        {
            return handlerValidation;
        }

        var resumedFlow = await flowManager.RunFlow(
            flowId,
            stepId ?? flowInstance.CurrentStep?.StepId,
            userInput);

        return Ok(resumedFlow);
    }

    [HttpGet("{flowId}")]
    public async Task<IActionResult> GetFlowState(string flowId)
    {
        logger.LogInformation("Getting flow state: {FlowId}", flowId);

        if (string.IsNullOrEmpty(flowId))
        {
            return BadRequest("Flow ID must be provided.");
        }

        var flowInstance = await flowRepository.FindById(flowId);

        if (flowInstance == null)
        {
            return NotFound($"Flow {flowId} not found.");
        }

        return Ok(flowInstance);
    }

    [HttpGet("{flowId}/result")]
    public async Task<IActionResult> GetFlowResult(string flowId)
    {
        logger.LogInformation("Getting flow result: {FlowId}", flowId);

        if (string.IsNullOrEmpty(flowId))
        {
            return BadRequest("Flow ID must be provided.");
        }

        var flowInstance = await flowRepository.FindById(flowId);

        if (flowInstance?.Context?.Output == null)
        {
            return NotFound($"Flow {flowId} not found or has no output.");
        }

        return Ok(flowInstance.Context.Output);
    }

    private bool TryGetHandlerType(string flowName, out Type? handlerType)
    {
        return _registeredHandlers.TryGetValue(flowName, out handlerType);
    }

    private IActionResult? ValidateHandlerType(Type handlerType, string flowName)
    {
        var baseHandlerType = handlerType.BaseType;

        if (baseHandlerType is not { IsGenericType: true } ||
            baseHandlerType.GetGenericTypeDefinition() != typeof(PausableChainHandler<,,>))
        {
            logger.LogError("Handler {FlowName} is not a valid PausableChainHandler", flowName);
            return BadRequest($"Flow '{flowName}' is not a valid PausableChainHandler.");
        }

        return null;
    }

    private (object? Input, IActionResult? Error) DeserializeInput(
        JsonElement inputJson,
        Type inputType,
        string flowName)
    {
        try
        {
            var deserializedInput = inputJson.Deserialize(inputType,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (deserializedInput == null)
            {
                logger.LogError("Deserialized input is null for flow {FlowName}", flowName);
                return (null, BadRequest($"Could not deserialize input for flow '{flowName}' to type {inputType.Name}."));
            }

            return (deserializedInput, null);
        }
        catch (JsonException jsonEx)
        {
            logger.LogError(jsonEx, "Failed to deserialize input for flow {FlowName}", flowName);
            return (null, BadRequest($"Could not deserialize input for flow '{flowName}' to type {inputType.Name}. {jsonEx.Message}"));
        }
    }

    private MethodInfo? CreateStartFlowMethod(Type handlerType, Type baseHandlerType)
    {
        var genericArguments = baseHandlerType.GetGenericArguments();
        return typeof(FlowManager)
            .GetMethod(nameof(FlowManager.StartFlow))?
            .MakeGenericMethod(handlerType, genericArguments[0], genericArguments[1], genericArguments[2]);
    }

    private async Task<(FlowInstance? Instance, IActionResult? Error)> ExecuteStartFlow(
        MethodInfo startMethod,
        object input,
        string flowName)
    {
        try
        {
            var task = (Task?)startMethod.Invoke(flowManager, [input]);

            if (task == null)
            {
                logger.LogError("StartFlow returned null task for flow {FlowName}", flowName);
                return (null, StatusCode(500, "Could not invoke StartFlow."));
            }

            await task;

            var flowInstance = ExtractFlowInstanceFromTask(task);
            if (flowInstance == null)
            {
                logger.LogError("Could not extract FlowInstance from task for flow {FlowName}", flowName);
                return (null, StatusCode(500, "Result was not a FlowInstance."));
            }

            return (flowInstance, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to execute StartFlow for {FlowName}", flowName);
            return (null, StatusCode(500, $"An error occurred during flow execution: {ex.Message}"));
        }
    }

    private FlowInstance? ExtractFlowInstanceFromTask(Task task)
    {
        var taskType = task.GetType();

        if (!taskType.IsGenericType)
        {
            return null;
        }

        var resultProperty = taskType.GetProperty("Result");
        var resultValue = resultProperty?.GetValue(task);

        return resultValue as FlowInstance;
    }

    private JsonElement? NormalizeUserInput(JsonElement? userInputJson)
    {
        return userInputJson?.ValueKind == JsonValueKind.Undefined || userInputJson == null
            ? null
            : userInputJson;
    }

    private IActionResult? ValidateFlowHandler(FlowInstance flowInstance, string flowId)
    {
        var handlerType = Type.GetType(flowInstance.FlowHandlerName);

        if (handlerType == null || !_registeredHandlers.ContainsValue(handlerType))
        {
            logger.LogError(
                "Handler type {HandlerName} for flow {FlowId} is not registered",
                flowInstance.FlowHandlerName, flowId);
            return BadRequest($"Handler type {flowInstance.FlowHandlerName} for flow {flowId} is not registered.");
        }

        var baseHandlerType = handlerType.BaseType;
        if (baseHandlerType == null || !baseHandlerType.IsGenericType ||
            baseHandlerType.GetGenericTypeDefinition() != typeof(PausableChainHandler<,,>))
        {
            return BadRequest($"Handler '{flowInstance.FlowHandlerName}' is not a valid PausableChainHandler.");
        }

        return null;
    }
}

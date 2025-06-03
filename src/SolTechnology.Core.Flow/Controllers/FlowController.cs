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
                var baseHandlerType = handlerType.BaseType;
                if (baseHandlerType is not { IsGenericType: true } ||
                    baseHandlerType.GetGenericTypeDefinition() != typeof(PausableChainHandler<,,>))
                {
                    return StatusCode(400, $"Flow '{flowName}' is not a valid PausableChainHandler.");
                }

                Type inputType = baseHandlerType.GetGenericArguments()[0];

                object? typedInitialInput;
                try
                {
                    typedInitialInput = initialInputJson.Deserialize(inputType,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                }
                catch (JsonException jsonEx)
                {
                    //TODO: bad request should not be success xd
                    return BadRequest(
                        $"Could not deserialize input for flow '{flowName}' to type {inputType.Name}. {jsonEx.Message}");
                }

                if (typedInitialInput == null)
                {
                    return BadRequest($"Could not deserialize input for flow '{flowName}' to type {inputType.Name}.");
                }

                MethodInfo? startMethod = typeof(FlowManager).GetMethod(nameof(FlowManager.StartFlow))?
                    .MakeGenericMethod(handlerType, inputType, baseHandlerType.GetGenericArguments()[1],
                        baseHandlerType.GetGenericArguments()[2]);

                if (startMethod == null)
                {
                    return StatusCode(400, "Could not make generic StartFlow method.");
                }

                var task = (Task?)startMethod.Invoke(flowManager, [typedInitialInput]);
                if (task == null) return StatusCode(400, "Could not invoke StartFlow.");

                await task;

                var resultProperty = task.GetType().GetProperty("Result");
                var flowInstance = resultProperty?.GetValue(task) as FlowInstance;

                return Ok(flowInstance);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error starting flow {FlowHandlerName}.", flowName);
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

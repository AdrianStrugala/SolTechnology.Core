using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Journey.Models;
using SolTechnology.Core.Journey.Workflow;
using SolTechnology.Core.Journey.Workflow.ChainFramework;
using SolTechnology.Core.Journey.Workflow.Persistence;


namespace SolTechnology.Core.Journey.Controllers
{
    [ApiController]
    [Route("api/journey")]
    public abstract class JourneyController(
        JourneyManager journeyManager,
        ILogger<JourneyController> logger,
        IJourneyInstanceRepository journeyRepository,
        IEnumerable<IJourneyHandler> journeyHandlers)
        : ControllerBase
    {

        private readonly Dictionary<string, Type> _registeredHandlers = journeyHandlers.ToDictionary(
            x => x.GetType().Name, y => y.GetType());

        [HttpPost("{flowName}/start")]
        public async Task<IActionResult> StartFlow(string flowName, [FromBody] JsonElement initialInputJson)
        {
            logger.LogInformation("Attempting to start journey with handler: {JourneyHandlerName}", flowName);

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

                MethodInfo? startMethod = typeof(JourneyManager).GetMethod(nameof(JourneyManager.StartFlow))?
                    .MakeGenericMethod(handlerType, inputType, baseHandlerType.GetGenericArguments()[1],
                        baseHandlerType.GetGenericArguments()[2]);

                if (startMethod == null)
                {
                    return StatusCode(400, "Could not make generic StartFlow method.");
                }

                var task = (Task?)startMethod.Invoke(journeyManager, [typedInitialInput]);
                if (task == null) return StatusCode(400, "Could not invoke StartFlow.");

                await task;

                var resultProperty = task.GetType().GetProperty("Result");
                var journeyInstance = resultProperty?.GetValue(task) as FlowInstance;

                return Ok(journeyInstance);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error starting journey {JourneyHandlerName}.", flowName);
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

                var flowInstance = await journeyRepository.FindById(flowId);
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

                flowInstance = await journeyManager.RunFlow(
                    flowId,
                    stepId ?? flowInstance.CurrentStep?.StepId,
                    userInput);

                return Ok(flowInstance);
        }

        [HttpGet("{journeyId}")]
        public async Task<IActionResult> GetJourneyState(string journeyId)
        {
            logger.LogInformation("Attempting to get status for journey: {JourneyId}", journeyId);
            if (string.IsNullOrEmpty(journeyId))
            {
                return BadRequest("Journey ID must be provided.");
            }

            var journeyInstance = await journeyRepository.FindById(journeyId);
            return Ok(journeyInstance);
        }
        
        [HttpGet("{journeyId}/result")]
        public async Task<IActionResult> GetJourneyResult(string journeyId)
        {
            logger.LogInformation("Attempting to get result for journey: {JourneyId}", journeyId);
            if (string.IsNullOrEmpty(journeyId))
            {
                return BadRequest("Journey ID must be provided.");
            }

            var journeyInstance = await journeyRepository.FindById(journeyId);
            return Ok(journeyInstance!.Context!.Output);
        }
    }
}

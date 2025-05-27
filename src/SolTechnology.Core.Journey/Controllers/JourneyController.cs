using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Journey.Models;
using SolTechnology.Core.Journey.Workflow;
using SolTechnology.Core.Journey.Workflow.ChainFramework;
using SolTechnology.Core.Journey.Workflow.Persistence;

// For JsonElement

// For MethodInfo

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
        public async Task<IActionResult> StartJourney(string flowName, [FromBody] JsonElement initialInputJson)
        {
            logger.LogInformation("Attempting to start journey with handler: {JourneyHandlerName}", flowName);

            if (!_registeredHandlers.TryGetValue(flowName, out Type? handlerType))
            {
                return NotFound($"Flow '{flowName}' not registered.");
            }

            try
            {
                var baseHandlerType = handlerType.BaseType;
                if (baseHandlerType is not { IsGenericType: true } || baseHandlerType.GetGenericTypeDefinition() != typeof(PausableChainHandler<,,>))
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
                    return BadRequest($"Could not deserialize input for flow '{flowName}' to type {inputType.Name}. {jsonEx.Message}");
                }
                if (typedInitialInput == null)
                {
                    return BadRequest($"Could not deserialize input for flow '{flowName}' to type {inputType.Name}.");
                }

                MethodInfo? startMethod = typeof(JourneyManager).GetMethod(nameof(JourneyManager.StartFlow))?
                    .MakeGenericMethod(handlerType, inputType, baseHandlerType.GetGenericArguments()[1], baseHandlerType.GetGenericArguments()[2]);

                if (startMethod == null)
                {
                    return StatusCode(400, "Could not make generic StartFlow method.");
                }

                var task = (Task?)startMethod.Invoke(journeyManager, [typedInitialInput]);
                if (task == null) return StatusCode(400, "Could not invoke StartFlow.");
                
                await task;

                var resultProperty = task.GetType().GetProperty("Result");
                var journeyInstance = resultProperty?.GetValue(task) as JourneyInstance;

                return BuildJourneyResponse(journeyInstance);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error starting journey {JourneyHandlerName}.", flowName);
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("{journeyId}")]
        public async Task<IActionResult> ResumeJourney(string journeyId, [FromBody] JsonElement? userInputJson) 
        {
            logger.LogInformation("Attempting to resume journey: {JourneyId}", journeyId);
            
            try
            {
                object? userInput = userInputJson?.ValueKind == JsonValueKind.Undefined || userInputJson == null ? null : userInputJson;

                var tempInstance = await journeyRepository.GetByIdAsync(journeyId); 
                if (tempInstance == null) return NotFound($"Journey {journeyId} not found.");
                
                if (string.IsNullOrEmpty(tempInstance.FlowHandlerName))
                {
                    logger.LogError("FlowHandlerName is null or empty for Journey {JourneyId}.", journeyId);
                    return StatusCode(500, $"FlowHandlerName is missing for journey {journeyId}.");
                }
                Type? handlerType = Type.GetType(tempInstance.FlowHandlerName);

                if (handlerType == null || !_registeredHandlers.ContainsValue(handlerType)) {
                     logger.LogError("Handler type {HandlerName} for journey {JourneyId} is not registered or cannot be resolved.", tempInstance.FlowHandlerName, journeyId);
                     return StatusCode(500, $"Handler type {tempInstance.FlowHandlerName} for journey {journeyId} is not registered or cannot be resolved.");
                }

                var baseHandlerType = handlerType.BaseType;
                if (baseHandlerType == null || !baseHandlerType.IsGenericType || baseHandlerType.GetGenericTypeDefinition() != typeof(PausableChainHandler<,,>))
                {
                    return StatusCode(500, $"Handler '{tempInstance.FlowHandlerName}' is not a valid PausableChainHandler.");
                }
                
                // The JourneyManager.ResumeJourneyAsync method is now non-generic based on Turn 57.
                // So, direct call is possible, passing userInput as object.
                // The reflection to call a generic version is not needed if JourneyManager.ResumeJourneyAsync itself handles the types.
                // The prompt for this current task provided a generic signature for JourneyManager.ResumeJourneyAsync
                // Let's stick to the prompt's request to reflectively call a generic version.

                MethodInfo? resumeMethod = typeof(JourneyManager)
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name == "ResumeJourneyAsync" && m.IsGenericMethodDefinition && m.GetGenericArguments().Length == 4);

                if (resumeMethod == null) return StatusCode(500, "Could not find generic ResumeJourneyAsync method on JourneyManager.");
                
                MethodInfo genericResumeMethod = resumeMethod.MakeGenericMethod(handlerType, baseHandlerType.GetGenericArguments()[0], baseHandlerType.GetGenericArguments()[1], baseHandlerType.GetGenericArguments()[2]);

                var task = (Task?)genericResumeMethod.Invoke(journeyManager, new[] { journeyId, userInput, null /*targetStepId*/ });
                if (task == null) return StatusCode(500, "Could not invoke ResumeJourneyAsync.");
                await task;

                var resultProperty = task.GetType().GetProperty("Result");
                var journeyInstance = resultProperty?.GetValue(task) as JourneyInstance;

                return BuildJourneyResponse(journeyInstance);
            }
            catch (KeyNotFoundException knfex)
            {
                return NotFound(knfex.Message);
            }
            catch (ArgumentException argex)
            {
                return BadRequest(argex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error resuming journey {JourneyId}.", journeyId);
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet("{journeyId}")]
        public async Task<IActionResult> GetJourneyStatus(string journeyId)
        {
            logger.LogInformation("Attempting to get status for journey: {JourneyId}", journeyId);
            if (string.IsNullOrEmpty(journeyId))
            {
                return BadRequest("Journey ID must be provided.");
            }
            var journeyInstance = await journeyRepository.GetByIdAsync(journeyId); 
            return BuildJourneyResponse(journeyInstance);
        }

        private IActionResult BuildJourneyResponse(JourneyInstance? journeyInstance)
        {
            if (journeyInstance == null)
            {
                return NotFound();
            }
            
            ChainContext<object, object>? genericContext = null;
            if (journeyInstance.ContextData != null)
            {
                try
                {
                    var contextType = journeyInstance.ContextData.GetType();
                    var statusProp = contextType.GetProperty("Status")?.GetValue(journeyInstance.ContextData);
                    var currentStepIdProp = contextType.GetProperty("CurrentStepId")?.GetValue(journeyInstance.ContextData)?.ToString();
                    var errorMsgProp = contextType.GetProperty("ErrorMessage")?.GetValue(journeyInstance.ContextData)?.ToString();
                    var historyProp = contextType.GetProperty("History")?.GetValue(journeyInstance.ContextData) as List<StepInfo>;
                    var outputProp = contextType.GetProperty("Output")?.GetValue(journeyInstance.ContextData);

                    genericContext = new ChainContext<object, object> 
                    {
                        Status = statusProp is FlowStatus fs ? fs : journeyInstance.Status, 
                        CurrentStepId = currentStepIdProp ?? string.Empty,
                        ErrorMessage = errorMsgProp,
                        History = historyProp ?? new List<StepInfo>(),
                        Output = outputProp ?? new object() 
                    };
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error trying to interpret ContextData for Journey {JourneyId}.", journeyInstance.JourneyId);
                    genericContext = new ChainContext<object, object> { Status = journeyInstance.Status, ErrorMessage = "Error interpreting context." };
                }
            }


            if (genericContext != null && genericContext.Status == FlowStatus.WaitingForInput)
            {
                Dictionary<string, Type>? requiredSchema = null;
                string? currentStepForUI = genericContext.CurrentStepId;

                var lastPausedStepInfo = genericContext.History
                    .LastOrDefault(h => h.StepId == currentStepForUI && h.Status == FlowStatus.WaitingForInput && h.OutputData != null);
                
                if(lastPausedStepInfo != null)
                {
                    if (lastPausedStepInfo.OutputData.TryGetValue("RequiredInputSchemaForPause", out var schemaObj) && schemaObj is Dictionary<string, Type> schema)
                    {
                        requiredSchema = schema;
                    }
                }

                return Ok(new
                {
                    journeyInstance.JourneyId,
                    currentStep = new
                    {
                        stepId = currentStepForUI,
                        type = "UI", 
                        requiredDataSchema = requiredSchema?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Name), 
                        message = genericContext.ErrorMessage 
                    },
                    status = genericContext.Status.ToString(),
                    lastUpdatedAt = journeyInstance.LastUpdatedAt
                });
            }

            if (genericContext != null && genericContext.Status == FlowStatus.Completed)
            {
                 return Ok(new
                {
                    journeyInstance.JourneyId,
                    currentStepId = genericContext.CurrentStepId, 
                    status = journeyInstance.Status.ToString(), 
                    errorMessage = genericContext.ErrorMessage,
                    lastUpdatedAt = journeyInstance.LastUpdatedAt,
                    output = genericContext.Output 
                });
            }

            return Ok(new
            {
                journeyInstance.JourneyId,
                currentStepId = genericContext?.CurrentStepId, 
                status = journeyInstance.Status.ToString(), 
                errorMessage = genericContext?.ErrorMessage,
                lastUpdatedAt = journeyInstance.LastUpdatedAt
            });
        }
    }
}

using System.Text.Json;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.CQRS.Errors;
using SolTechnology.Core.Flow.Models;

namespace SolTechnology.Core.Flow.Workflow.ChainFramework
{
    public interface IFlowHandler;

    public abstract class PausableChainHandler<TInput, TContext, TOutput>(
        IServiceProvider serviceProvider,
        ILogger<PausableChainHandler<TInput, TContext, TOutput>> logger)
        : IFlowHandler
        where TInput : new()
        where TOutput : new()
        where TContext : FlowContext<TInput, TOutput>, new()
    {
        private List<StepInfo> _stepHistory = new();
        private CancellationToken _cancellationToken;
        private string? _requestedStep;
        private JsonElement? _stepInput;
        private TContext _context = null!;
        private StepInfo? _currentStep;

        private bool _flowFailed;
        private bool _flowPaused;


        protected abstract Task HandleChainDefinition(TContext context);

        public async Task<FlowInstance> ExecuteHandler(
            FlowInstance flowInstance,
            TContext context,
            string? requestedStepId = null,
            JsonElement? requestedStepInput = null,
            CancellationToken cancellationToken = default)
        {
            _cancellationToken = cancellationToken;

            logger.LogInformation(
                "Executing handler for context {ContextType}, requested step: {RequestedStepId}",
                typeof(TContext).Name,
                requestedStepId);

            _context = context;
            _requestedStep = requestedStepId;
            _stepInput = requestedStepInput;

            if (_requestedStep != null)
            {
                logger.LogInformation(
                    "Resuming from step {CurrentStep}",
                    requestedStepId);
            }

            await HandleChainDefinition(context);

            if (_flowFailed)
            {
                flowInstance.Status = FlowStatus.Failed;
            }
            else if(_currentStep != null)
            {
                flowInstance.CurrentStep = _currentStep;
                flowInstance.Status = FlowStatus.WaitingForInput;
            }
            else
            {
                flowInstance.Status = FlowStatus.Completed;
            }
            
            flowInstance.History.AddRange(_stepHistory);
            flowInstance.Context = context;
            
            logger.LogInformation(
                @"
                Handler execution finished for context {ContextType}.
                Status: {Status}, 
                CurrentStep: {CurrentStepId}",
                
                typeof(TContext).Name, 
                flowInstance.Status, 
                flowInstance.CurrentStep);

            return flowInstance;
        }

        protected async Task Invoke<TStep>() where TStep : IFlowStep<TContext>
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var stepType = typeof(TStep);
            if (serviceProvider.GetService(stepType) is not IFlowStep<TContext> stepInstance)
            {
                throw new InvalidOperationException($"Could not resolve service for type {stepType.Name}");
            }

            if (_flowFailed)
            {
                logger.LogInformation($"Skipping step: [{stepInstance.StepId}]. Flow in error state");
                return;
            }

            if (_requestedStep != null && stepInstance.StepId != _requestedStep || _flowPaused)
            {
                logger.LogInformation($"Skipping step: [{stepInstance.StepId}]");
                return;
            }

            _currentStep = new StepInfo
            {
                StartedAt = DateTime.UtcNow,
                Status = FlowStatus.Running,
                StepId = stepInstance.StepId
            };

            Result stepResult = null!;

            var interactiveStepType = GetInteractiveStepBaseType(stepType);
            if (interactiveStepType != null)
            {
                _currentStep.StepType = "Interactive";

                // invoke GetRequiredInputSchema via reflection 
                var method =
                    interactiveStepType.GetMethod(nameof(InteractiveFlowStep<object, object>.GetRequiredInputSchema))!;
                _currentStep.RequiredData = (List<DataField>)method.Invoke(stepInstance, [])!;

                object? stepInput = TryDeserializeInput(interactiveStepType);
                if (stepInput == null)
                {
                    _flowPaused = true;
                    _currentStep.Status = FlowStatus.WaitingForInput;
                    return;
                }

                _currentStep.ProvidedData = _stepInput;

                try
                {
                    // invoke ExecuteWithUserInput via reflection 
                    var executeMethod =
                        interactiveStepType.GetMethod(nameof(InteractiveFlowStep<object, object>.ExecuteWithUserInput))
                        !;
                    var task = (Task<Result>)executeMethod.Invoke(stepInstance, [_context, stepInput])!;
                    stepResult = await task;
                }
                catch (Exception e)
                {
                    _flowFailed = true;
                    _currentStep.Error = Error.From(e);
                    _currentStep.Status = FlowStatus.Failed;
                }

                if (stepResult.IsFailure)
                {
                    _flowFailed = true;
                    _currentStep.Error = stepResult.Error!;
                    _currentStep.Status = FlowStatus.Failed;
                }
            }
            else
            {
                //Step is automated
                _currentStep.StepType = "Automated";

                try
                {
                    stepResult = await stepInstance.Execute(_context);
                }
                catch (Exception e)
                {
                    _flowFailed = true;
                    _currentStep.Error = Error.From(e);
                    _currentStep.Status = FlowStatus.Failed;
                }

                if (stepResult.IsFailure)
                {
                    _flowFailed = true;
                    _currentStep.Error = stepResult.Error!;
                    _currentStep.Status = FlowStatus.Failed;
                }
            }

            _currentStep.FinishedAt = DateTime.UtcNow;
            if (!_flowFailed)
            {
                _currentStep.Status = FlowStatus.Completed;
            }
            _stepHistory.Add(_currentStep);
            _currentStep = null;
        }

        private Type? GetInteractiveStepBaseType(Type? stepType)
        {
            while (stepType != null && stepType != typeof(object))
            {
                if (stepType.IsGenericType &&
                    stepType.GetGenericTypeDefinition() == typeof(InteractiveFlowStep<,>))
                {
                    return stepType;
                }

                stepType = stepType.BaseType;
            }

            return null;
        }

        private object? TryDeserializeInput(Type interactiveStepType)
        {
            if (_stepInput == null)
            {
                return null;
            }

            var stepInputType = interactiveStepType.GetGenericArguments()[1];

            // ) try to deserialize
            try
            {
                return _stepInput.Value.Deserialize(
                    stepInputType,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
            }
            catch (JsonException)
            {
                logger.LogWarning("Provided invalid data: [{Data}]", _stepInput);
                return null;
            }
        }
    }
}

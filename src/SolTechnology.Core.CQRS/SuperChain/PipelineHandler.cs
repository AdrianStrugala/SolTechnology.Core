using MediatR;
using SolTechnology.Core.CQRS;

public abstract class PipelineHandler<TInput, TContext, TOutput>(IServiceProvider serviceProvider)
    : IQueryHandler<TInput, TOutput>
    where TInput : IRequest<Result<TOutput>>
    where TContext : PipelineContext<TInput, TOutput>, new()
{
    protected internal TContext Context { get; set; } = null!;
    private readonly List<Type> _steps = new();

    // Register a step in the pipeline.
    // TStep must implement IAsyncStep<PipelineContext<TInput, TOutput>>
    protected void Step<TStep>() where TStep : IAsyncStep<TContext>
    {
        _steps.Add(typeof(TStep));
    }

    // Execute all the registered steps sequentially.
    private async Task ExecutePipelineAsync()
    {
        foreach (var stepType in _steps)
        {
            // Resolve the step using the DI container.
            if (serviceProvider.GetService(stepType) is not IAsyncStep<TContext> stepInstance)
            {
                throw new InvalidOperationException($"Could not resolve service for type {stepType.Name}");
            }
            await stepInstance.Execute(Context);
        }
    }

    // Derived classes implement this to register their specific steps.
    protected abstract void RegisterSteps();

    // Public entry point: registers steps, executes the pipeline, and returns the Output.
    public async Task<Result<TOutput>> Handle(TInput input, CancellationToken cancellationToken = default)
    {
        Context = new TContext { Input = input };
        RegisterSteps();
        await ExecutePipelineAsync();
        return Context.Output;
    }
}
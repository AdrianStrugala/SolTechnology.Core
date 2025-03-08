using MediatR;
using SolTechnology.Core.CQRS;

public abstract class PipelineHandler<TInput, TOutput> : IQueryHandler<TInput, TOutput> where TInput : IRequest<Result<TOutput>>
{
    private readonly IServiceProvider _serviceProvider;
    protected internal PipelineContext<TInput, TOutput> Context { get; set; } = null!;
    private readonly List<Type> _steps = new();

    protected PipelineHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    // Register a step in the pipeline.
    // TStep must implement IAsyncStep<PipelineContext<TInput, TOutput>>
    protected void Step<TStep>()
    {
        var stepType = typeof(TStep);
        var valid = stepType.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAsyncStep<>))
            .Any(i =>
            {
                var arg = i.GetGenericArguments()[0];
                return typeof(PipelineContext<TInput, TOutput>).IsAssignableFrom(arg);
            });
        
        if (!valid)
        {
            throw new InvalidOperationException(
                $"{stepType.Name} must implement IAsyncStep<PipelineContext<TInput, TOutput>> (or a derived context)");
        }
        
        _steps.Add(typeof(TStep));
    }

    // Execute all the registered steps sequentially.
    private async Task ExecutePipelineAsync()
    {
        foreach (var stepType in _steps)
        {
            // Resolve the step using the DI container.
            var stepInstance = _serviceProvider.GetService(stepType) as IAsyncStep<PipelineContext<TInput, TOutput>>;
            if (stepInstance == null)
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
        Context = new PipelineContext<TInput, TOutput> { Input = input };
        RegisterSteps();
        await ExecutePipelineAsync();
        return Context.Output;
    }
}
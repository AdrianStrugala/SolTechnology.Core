using MediatR;

namespace SolTechnology.Core.CQRS.SuperChain;

public abstract class ChainHandler<TInput, TContext, TOutput>(IServiceProvider serviceProvider)
    : IQueryHandler<TInput, TOutput>
    where TInput : IRequest<Result<TOutput>>
    where TContext : ChainContext<TInput, TOutput>, new()
{
    protected internal TContext Context { get; set; } = null!;

    // Execute a single chain step immediately.
    protected async Task Invoke<TStep>() where TStep : IChainStep<TContext>
    {
        if (serviceProvider.GetService(typeof(TStep)) is not IChainStep<TContext> stepInstance)
        {
            throw new InvalidOperationException($"Could not resolve service for type {typeof(TStep).Name}");
        }
        await stepInstance.Execute(Context);
    }

    
    // Derived classes implement this method.
    // Use this to call Step<TChainStep>() sequentially.
    protected abstract Task HandleChain();

    // Public entry point: registers steps, executes the pipeline, and returns the Output.
    public async Task<Result<TOutput>> Handle(TInput input, CancellationToken cancellationToken = default)
    {
        Context = new TContext { Input = input };
        await HandleChain();
        return Context.Output;
    }
}
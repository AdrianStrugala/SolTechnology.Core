using System.Diagnostics.CodeAnalysis;
using MediatR;

namespace SolTechnology.Core.CQRS.SuperChain;

/// <summary>
/// Provides an abstract base for creating chain-of-responsibility handlers with defined input, context, and output types.
/// </summary>
/// <typeparam name="TInput">The type of input handled, must implement MediatR IRequest&lt;Result&lt;TOutput&gt;&gt;.</typeparam>
/// <typeparam name="TContext">The type of context maintained across the execution chain, must inherit from ChainContext&lt;TInput, TOutput&gt;.</typeparam>
/// <typeparam name="TOutput">The type of output produced by the handler.</typeparam>
public abstract class ChainHandler<TInput, TContext, TOutput>(IServiceProvider serviceProvider)
    : IQueryHandler<TInput, TOutput>
    where TInput : IRequest<Result<TOutput>>
    where TContext : ChainContext<TInput, TOutput>, new()
{
    /// <summary>
    /// The context instance holding input, intermediate data, and the eventual output.
    /// </summary>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    protected internal TContext Context { get; set; } = null!;

    /// <summary>
    /// Invokes the specified chain step immediately.
    /// </summary>
    /// <typeparam name="TStep">The type of the chain step to invoke, must implement IChainStep&lt;TContext&gt;.</typeparam>
    /// <exception cref="InvalidOperationException">Thrown if the chain step cannot be resolved via the service provider.</exception>
    protected async Task Invoke<TStep>() where TStep : IChainStep<TContext>
    {
        if (serviceProvider.GetService(typeof(TStep)) is not IChainStep<TContext> stepInstance)
        {
            throw new InvalidOperationException($"Could not resolve service for type {typeof(TStep).Name}");
        }

        await stepInstance.Execute(Context);
    }

    /// <summary>
    /// Implement this method in derived classes to define the sequence of chain steps.
    /// Typically used to invoke steps using Invoke&lt;TChainStep&gt;().
    /// </summary>
    protected abstract Task HandleChain();

    /// <summary>
    /// Public entry point for executing the handler. Initializes context, executes defined chain steps, and returns the resulting output.
    /// </summary>
    /// <param name="input">The input provided to the handler.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous operation, containing the handler's output wrapped in a Result&lt;TOutput&gt;.</returns>
    public async Task<Result<TOutput>> Handle(TInput input, CancellationToken cancellationToken = default)
    {
        Context = new TContext { Input = input };
        await HandleChain();
        return Context.Output;
    }
}

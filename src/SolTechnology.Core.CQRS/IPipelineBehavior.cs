namespace SolTechnology.Core.CQRS;

/// <summary>
/// A step in the request processing pipeline. Behaviors are composed around the handler
/// in registration order (first registered = outermost).
/// </summary>
public interface IPipelineBehavior<in TRequest, TResponse>
{
    /// <summary>
    /// Runs this step around the rest of the pipeline. Call <paramref name="next"/> to continue, or
    /// return a response without calling it to short-circuit.
    /// </summary>
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}

/// <summary>
/// Represents the next step in the pipeline (either another behavior or the terminal handler).
/// </summary>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();


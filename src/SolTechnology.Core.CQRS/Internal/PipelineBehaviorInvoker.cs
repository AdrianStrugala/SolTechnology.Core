using System.Collections.Concurrent;
using System.Reflection;

namespace SolTechnology.Core.CQRS.Internal;

/// <summary>
/// Caches, per request type, a delegate that calls <see cref="IPipelineBehavior{TRequest,TResponse}.Handle"/>
/// through the interface, so explicit implementations work and synchronous throws are not wrapped.
/// </summary>
internal static class PipelineBehaviorInvoker<TResponse>
{
    private static readonly ConcurrentDictionary<Type, Func<object, object, RequestHandlerDelegate<TResponse>, CancellationToken, Task<TResponse>>> Invokers = new();

    private static readonly MethodInfo InvokeDefinition =
        typeof(PipelineBehaviorInvoker<TResponse>).GetMethod(nameof(Invoke), BindingFlags.NonPublic | BindingFlags.Static)!;

    public static Func<object, object, RequestHandlerDelegate<TResponse>, CancellationToken, Task<TResponse>> For(Type requestType)
        => Invokers.GetOrAdd(
            requestType,
            static type => InvokeDefinition.MakeGenericMethod(type)
                .CreateDelegate<Func<object, object, RequestHandlerDelegate<TResponse>, CancellationToken, Task<TResponse>>>());

    private static Task<TResponse> Invoke<TRequest>(
        object behavior, object request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        => ((IPipelineBehavior<TRequest, TResponse>)behavior).Handle((TRequest)request, next, cancellationToken);
}

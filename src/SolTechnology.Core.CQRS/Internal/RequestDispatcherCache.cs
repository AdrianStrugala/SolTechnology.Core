using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace SolTechnology.Core.CQRS.Internal;

/// <summary>
/// Caches a compiled dispatcher delegate per (request-type → handler-type) pair.
/// On the hot path only a dictionary lookup + delegate invocation is performed — no reflection.
/// </summary>
internal static class RequestDispatcherCache
{
    private static readonly ConcurrentDictionary<Type, RequestDispatcher> Dispatchers = new();

    public static RequestDispatcher GetOrAdd(Type requestType, Type responseType, Type handlerInterfaceType)
        => Dispatchers.GetOrAdd(requestType, _ => Build(requestType, responseType, handlerInterfaceType));

    private static RequestDispatcher Build(Type requestType, Type responseType, Type handlerInterfaceType)
    {
        // Find the Handle method on the handler interface
        var handleMethod = handlerInterfaceType.GetMethod("Handle", BindingFlags.Public | BindingFlags.Instance)!;

        // Parameters: (IServiceProvider sp, object request, CancellationToken ct) → Task<object>
        var spParam = Expression.Parameter(typeof(IServiceProvider), "sp");
        var reqParam = Expression.Parameter(typeof(object), "request");
        var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

        // var handler = (IHandler<T,R>) sp.GetService(typeof(IHandler<T,R>));
        var getServiceMethod = typeof(IServiceProvider).GetMethod("GetService")!;
        var handlerExpr = Expression.Convert(
            Expression.Call(spParam, getServiceMethod, Expression.Constant(handlerInterfaceType)),
            handlerInterfaceType);

        // handler.Handle((TRequest)request, ct)
        var callExpr = Expression.Call(
            handlerExpr,
            handleMethod,
            Expression.Convert(reqParam, requestType),
            ctParam);

        // We need to convert Task<Result<T>> to Task<object> — use a continuation helper
        var lambda = Expression.Lambda<Func<IServiceProvider, object, CancellationToken, Task<object>>>(
            Expression.Call(
                typeof(RequestDispatcherCache),
                nameof(CastTask),
                new[] { responseType },
                callExpr),
            spParam, reqParam, ctParam);

        return new RequestDispatcher(lambda.Compile());
    }

    /// <summary>
    /// Helper to convert Task&lt;T&gt; → Task&lt;object&gt; without blocking.
    /// </summary>
    internal static async Task<object> CastTask<T>(Task<T> task) => (object)(await task)!;
}

/// <summary>
/// A compiled dispatcher that invokes the handler for a specific request type.
/// </summary>
internal sealed class RequestDispatcher
{
    private readonly Func<IServiceProvider, object, CancellationToken, Task<object>> _invoke;

    public RequestDispatcher(Func<IServiceProvider, object, CancellationToken, Task<object>> invoke)
    {
        _invoke = invoke;
    }

    public Task<object> Invoke(IServiceProvider sp, object request, CancellationToken ct)
        => _invoke(sp, request, ct);
}


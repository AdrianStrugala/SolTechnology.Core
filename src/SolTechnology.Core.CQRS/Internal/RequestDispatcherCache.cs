using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace SolTechnology.Core.CQRS.Internal;

/// <summary>
/// Caches a compiled dispatcher delegate per (request type, handler interface) pair, so one request
/// type sent both as a query and as a command reaches the matching handler.
/// </summary>
internal static class RequestDispatcherCache
{
    private static readonly ConcurrentDictionary<(Type RequestType, Type HandlerInterfaceType), RequestDispatcher> Dispatchers = new();

    private static readonly MethodInfo ResolveHandlerMethod =
        typeof(RequestDispatcherCache).GetMethod(nameof(ResolveHandler), BindingFlags.NonPublic | BindingFlags.Static)!;

    public static RequestDispatcher GetOrAdd(Type requestType, Type responseType, Type handlerInterfaceType)
        => Dispatchers.GetOrAdd(
            (requestType, handlerInterfaceType),
            static (key, responseType) => Build(key.RequestType, responseType, key.HandlerInterfaceType),
            responseType);

    private static RequestDispatcher Build(Type requestType, Type responseType, Type handlerInterfaceType)
    {
        var handleMethod = handlerInterfaceType.GetMethod("Handle", BindingFlags.Public | BindingFlags.Instance)!;

        // (IServiceProvider sp, object request, CancellationToken ct) => CastTask(((IHandler)ResolveHandler(...)).Handle((TRequest)request, ct))
        var spParam = Expression.Parameter(typeof(IServiceProvider), "sp");
        var reqParam = Expression.Parameter(typeof(object), "request");
        var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

        var handlerExpr = Expression.Convert(
            Expression.Call(
                ResolveHandlerMethod,
                spParam,
                Expression.Constant(handlerInterfaceType),
                Expression.Constant(requestType)),
            handlerInterfaceType);

        var callExpr = Expression.Call(
            handlerExpr,
            handleMethod,
            Expression.Convert(reqParam, requestType),
            ctParam);

        var lambda = Expression.Lambda<Func<IServiceProvider, object, CancellationToken, Task<object>>>(
            Expression.Call(
                typeof(RequestDispatcherCache),
                nameof(CastTask),
                new[] { responseType },
                callExpr),
            spParam, reqParam, ctParam);

        return new RequestDispatcher(lambda.Compile());
    }

    private static object ResolveHandler(IServiceProvider serviceProvider, Type handlerInterfaceType, Type requestType)
        => serviceProvider.GetService(handlerInterfaceType) ?? throw MissingHandler(handlerInterfaceType, requestType);

    private static InvalidOperationException MissingHandler(Type handlerInterfaceType, Type requestType)
    {
        var registration = handlerInterfaceType.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)
            ? nameof(CQRSOptions.RegisterQueriesFromAssembly)
            : nameof(CQRSOptions.RegisterCommandsFromAssembly);

        return new InvalidOperationException(
            $"No handler is registered for request '{requestType.FullName}'. " +
            $"Add a public, non-abstract handler class and register its assembly with " +
            $"AddSolCQRS(o => o.{registration}(assembly)); internal handlers are not discovered.");
    }

    /// <summary>
    /// Helper to convert Task&lt;T&gt; → Task&lt;object&gt; without blocking.
    /// </summary>
    internal static async Task<object> CastTask<T>(Task<T> task) => (object)(await task)!;
}

/// <summary>
/// A compiled dispatcher that invokes the handler for a specific request type.
/// </summary>
internal sealed class RequestDispatcher(Func<IServiceProvider, object, CancellationToken, Task<object>> invoke)
{
    public Task<object> Invoke(IServiceProvider sp, object request, CancellationToken ct)
        => invoke(sp, request, ct);
}

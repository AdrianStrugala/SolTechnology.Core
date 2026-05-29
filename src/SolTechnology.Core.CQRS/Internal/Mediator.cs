using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.CQRS.Internal;

/// <summary>
/// In-house mediator implementation. Dispatches commands and queries through the pipeline
/// behavior chain, and publishes notifications fire-and-forget.
/// </summary>
internal sealed class CQRSMediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CQRSMediator> _logger;

    public CQRSMediator(IServiceProvider serviceProvider, IServiceScopeFactory scopeFactory, ILogger<CQRSMediator> logger)
    {
        _serviceProvider = serviceProvider;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task<Result> Send(ICommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return SendInternal<Result>(command, cancellationToken);
    }

    public Task<Result<TResult>> Send<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return SendInternal<Result<TResult>>(command, cancellationToken);
    }

    public Task<Result<TResult>> Send<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return SendInternal<Result<TResult>>(query, cancellationToken);
    }

    public void Publish<TNotification>(TNotification notification) where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        _ = Task.Run(async () =>
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var handlers = scope.ServiceProvider.GetServices<INotificationHandler<TNotification>>();
            foreach (var handler in handlers)
            {
                try
                {
                    await handler.Handle(notification, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Notification handler {HandlerType} failed for {NotificationType}",
                        handler.GetType().Name, typeof(TNotification).Name);
                }
            }
        });
    }

    public void Publish(INotification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);
        // Dispatch via reflection to the generic Publish<T> method
        var notificationType = notification.GetType();
        var method = typeof(CQRSMediator).GetMethod(nameof(Publish), 1, new[] { Type.MakeGenericMethodParameter(0) })!;
        var generic = method.MakeGenericMethod(notificationType);
        generic.Invoke(this, new object[] { notification });
    }

    private async Task<TResponse> SendInternal<TResponse>(object request, CancellationToken cancellationToken)
    {
        var requestType = request.GetType();
        var responseType = typeof(TResponse);

        // Determine the handler interface type
        var handlerInterfaceType = ResolveHandlerInterfaceType(requestType, responseType);

        // Get pipeline behaviors
        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, responseType);
        var behaviors = _serviceProvider.GetServices(behaviorType).Cast<object>().ToList();

        // Build the handler invocation as the innermost delegate
        var dispatcher = RequestDispatcherCache.GetOrAdd(requestType, responseType, handlerInterfaceType);
        RequestHandlerDelegate<TResponse> handler = async () => (TResponse)await dispatcher.Invoke(_serviceProvider, request, cancellationToken);

        // Fold behaviors right-to-left (last registered = innermost)
        for (var i = behaviors.Count - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var next = handler;
            var handleMethod = behavior.GetType().GetMethod("Handle")!;
            handler = () => (Task<TResponse>)handleMethod.Invoke(behavior, new object[] { request, next, cancellationToken })!;
        }

        return await handler();
    }

    private static Type ResolveHandlerInterfaceType(Type requestType, Type responseType)
    {
        // ICommand (no TResult) → ICommandHandler<TCommand>
        // ICommand<TResult> → ICommandHandler<TCommand, TResult>
        // IQuery<TResult> → IQueryHandler<TQuery, TResult>

        if (responseType == typeof(Result))
        {
            return typeof(ICommandHandler<>).MakeGenericType(requestType);
        }

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var resultInnerType = responseType.GetGenericArguments()[0];

            // Check if it's a query
            var queryInterface = requestType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>));

            if (queryInterface != null)
            {
                return typeof(IQueryHandler<,>).MakeGenericType(requestType, resultInnerType);
            }

            // It's a command with result
            return typeof(ICommandHandler<,>).MakeGenericType(requestType, resultInnerType);
        }

        throw new InvalidOperationException($"Cannot resolve handler for request type {requestType.Name} with response type {responseType.Name}");
    }
}





using Microsoft.Extensions.DependencyInjection;

namespace SolTechnology.Core.CQRS.Internal;

/// <summary>
/// In-house mediator implementation. Dispatches commands and queries through the pipeline
/// behavior chain, and publishes events via <see cref="IEventPublisher"/>.
/// </summary>
internal sealed class CQRSMediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEventPublisher _eventPublisher;

    public CQRSMediator(IServiceProvider serviceProvider, IEventPublisher eventPublisher)
    {
        _serviceProvider = serviceProvider;
        _eventPublisher = eventPublisher;
    }

    public Task<Result> Send(ICommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var handlerInterfaceType = typeof(ICommandHandler<>).MakeGenericType(command.GetType());
        return SendInternal<Result>(command, handlerInterfaceType, cancellationToken);
    }

    public Task<Result<TResult>> Send<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var handlerInterfaceType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));
        return SendInternal<Result<TResult>>(command, handlerInterfaceType, cancellationToken);
    }

    public Task<Result<TResult>> Send<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var handlerInterfaceType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));
        return SendInternal<Result<TResult>>(query, handlerInterfaceType, cancellationToken);
    }

    public void Publish<TEvent>(TEvent notification) where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(notification);
        _eventPublisher.Publish(notification);
    }

    public void Publish(IEvent notification)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _eventPublisher.Publish(notification);
    }

    private async Task<TResponse> SendInternal<TResponse>(object request, Type handlerInterfaceType, CancellationToken cancellationToken)
    {
        var requestType = request.GetType();

        var dispatcher = RequestDispatcherCache.GetOrAdd(requestType, typeof(TResponse), handlerInterfaceType);
        RequestHandlerDelegate<TResponse> pipeline = async () => (TResponse)await dispatcher.Invoke(_serviceProvider, request, cancellationToken);

        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse));
        var behaviors = _serviceProvider.GetServices(behaviorType).Cast<object>().ToList();
        var invokeBehavior = PipelineBehaviorInvoker<TResponse>.For(requestType);

        // Fold right-to-left so the first registered behavior is the outermost.
        for (var i = behaviors.Count - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var next = pipeline;
            pipeline = () => invokeBehavior(behavior, request, next, cancellationToken);
        }

        return await pipeline();
    }
}

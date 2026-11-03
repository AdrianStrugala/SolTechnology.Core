using System.Collections.Concurrent;
using SolTechnology.Core;
using SolTechnology.Core.CQRS;

namespace SolTechnology.Core.CQRS.Tests;

// --- Requests without a discoverable handler ---

public class UnhandledCommand : ICommand;

public class InternalHandlerQuery : IQuery<string>;

internal sealed class InternalHandlerQueryHandler : IQueryHandler<InternalHandlerQuery, string>
{
    public Task<Result<string>> Handle(InternalHandlerQuery query, CancellationToken cancellationToken)
        => Task.FromResult(Result<string>.Success("never discovered"));
}

// --- One request type sent both as a query and as a command ---

public class QueryAndCommandRequest : IQuery<string>, ICommand<string>;

public class QueryAndCommandRequestQueryHandler : IQueryHandler<QueryAndCommandRequest, string>
{
    public Task<Result<string>> Handle(QueryAndCommandRequest query, CancellationToken cancellationToken)
        => Task.FromResult(Result<string>.Success("query"));
}

public class QueryAndCommandRequestCommandHandler : ICommandHandler<QueryAndCommandRequest, string>
{
    public Task<Result<string>> Handle(QueryAndCommandRequest command, CancellationToken cancellationToken)
        => Task.FromResult(Result<string>.Success("command"));
}

// --- Synchronous throws ---

public class SyncThrowingCommand : ICommand;

public class SyncThrowingCommandHandler : ICommandHandler<SyncThrowingCommand>
{
    public Task<Result> Handle(SyncThrowingCommand command, CancellationToken cancellationToken)
        => throw new InvalidOperationException("handler threw synchronously");
}

public sealed class SyncThrowingBehavior : IPipelineBehavior<TestCommand, Result>
{
    public Task<Result> Handle(TestCommand request, RequestHandlerDelegate<Result> next, CancellationToken cancellationToken)
        => throw new InvalidOperationException("behavior threw synchronously");
}

// --- Behaviors that record their invocation ---

public class BehaviorLog
{
    public ConcurrentQueue<string> Entries { get; } = new();
}

public sealed class ExplicitInterfaceBehavior(BehaviorLog log) : IPipelineBehavior<TestCommand, Result>
{
    Task<Result> IPipelineBehavior<TestCommand, Result>.Handle(
        TestCommand request, RequestHandlerDelegate<Result> next, CancellationToken cancellationToken)
    {
        log.Entries.Enqueue("explicit");
        return next();
    }
}

public sealed class FirstRecordingBehavior(BehaviorLog log) : IPipelineBehavior<TestCommand, Result>
{
    public async Task<Result> Handle(TestCommand request, RequestHandlerDelegate<Result> next, CancellationToken cancellationToken)
    {
        log.Entries.Enqueue("first:before");
        var result = await next();
        log.Entries.Enqueue("first:after");
        return result;
    }
}

public sealed class SecondRecordingBehavior(BehaviorLog log) : IPipelineBehavior<TestCommand, Result>
{
    public async Task<Result> Handle(TestCommand request, RequestHandlerDelegate<Result> next, CancellationToken cancellationToken)
    {
        log.Entries.Enqueue("second:before");
        var result = await next();
        log.Entries.Enqueue("second:after");
        return result;
    }
}

// --- Event handler with an explicit interface implementation ---

public class ExplicitHandlerEvent : IEvent
{
    public string Message { get; set; } = "explicit";
}

public class ExplicitInterfaceEventHandler(NotificationLog log) : IEventHandler<ExplicitHandlerEvent>
{
    Task IEventHandler<ExplicitHandlerEvent>.Handle(ExplicitHandlerEvent notification, CancellationToken cancellationToken)
    {
        log.Messages.Add(notification.Message);
        return Task.CompletedTask;
    }
}

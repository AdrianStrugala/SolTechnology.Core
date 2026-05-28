using FluentValidation;
using SolTechnology.Core.CQRS;

namespace SolTechnology.Core.CQRS.Tests;

// --- Queries ---

public class TestQuery : IQuery<string>
{
    public string Input { get; set; } = "test";
}

public class TestQueryHandler : IQueryHandler<TestQuery, string>
{
    public Task<Result<string>> Handle(TestQuery query, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<string>.Success($"Hello {query.Input}"));
    }
}

// --- Commands ---

public class TestCommand : ICommand
{
    public string Name { get; set; } = "cmd";
}

public class TestCommandHandler : ICommandHandler<TestCommand>
{
    public Task<Result> Handle(TestCommand command, CancellationToken cancellationToken)
    {
        return Result.SuccessAsTask();
    }
}

public class TestCommandWithResult : ICommand<int>
{
    public int Value { get; set; }
}

public class TestCommandWithResultHandler : ICommandHandler<TestCommandWithResult, int>
{
    public Task<Result<int>> Handle(TestCommandWithResult command, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<int>.Success(command.Value * 2));
    }
}

// --- Validated command ---

public class ValidatedCommand : ICommand
{
    public string Name { get; set; } = null!;
}

public class ValidatedCommandValidator : AbstractValidator<ValidatedCommand>
{
    public ValidatedCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(5);
    }
}

public class ValidatedCommandHandler : ICommandHandler<ValidatedCommand>
{
    public static bool WasCalled { get; set; }

    public Task<Result> Handle(ValidatedCommand command, CancellationToken cancellationToken)
    {
        WasCalled = true;
        return Result.SuccessAsTask();
    }
}

// --- Notifications ---

public class TestNotification : INotification
{
    public string Message { get; set; } = "event";
}

/// <summary>Shared state singleton — survives across scopes.</summary>
public class NotificationLog
{
    public System.Collections.Concurrent.ConcurrentBag<string> Messages { get; } = new();
}

public class TestNotificationHandler : INotificationHandler<TestNotification>
{
    private readonly NotificationLog _log;
    public TestNotificationHandler(NotificationLog log) => _log = log;

    public Task Handle(TestNotification notification, CancellationToken cancellationToken)
    {
        _log.Messages.Add(notification.Message);
        return Task.CompletedTask;
    }
}

public class ThrowingNotificationHandler : INotificationHandler<TestNotification>
{
    public Task Handle(TestNotification notification, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Handler exploded");
    }
}



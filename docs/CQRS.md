### Overview

The SolTechnology.Core.CQRS library provides a complete CQRS (Command Query Responsibility Segregation) implementation with an in-house mediator, Result pattern for explicit success/failure handling, automatic FluentValidation, fire-and-forget notifications, and logging pipeline behaviors.

### Registration

```csharp
services.AddCQRS();

// Or with explicit assembly scanning and options:
services.AddCQRS(
    o => o.UseFluentValidation = true,
    typeof(Program).Assembly);
```

### Defining a Command

```csharp
// Command with no return data (side-effect only)
public class CreateUserCommand : ICommand
{
    public string Name { get; set; }
    public string Email { get; set; }
}

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand>
{
    public async Task<Result> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        await _userRepository.Create(command.Name, command.Email);
        return Result.Success();
    }
}

// Command with return data
public class CreateOrderCommand : ICommand<int>
{
    public string Product { get; set; }
}

public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, int>
{
    public async Task<Result<int>> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var orderId = await _orderRepository.Create(command.Product);
        return Result<int>.Success(orderId);
    }
}
```

### Defining a Query

```csharp
public class GetUserQuery : IQuery<User>
{
    public int UserId { get; set; }
}

public class GetUserQueryHandler : IQueryHandler<GetUserQuery, User>
{
    public async Task<Result<User>> Handle(GetUserQuery query, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetById(query.UserId);

        if (user == null)
            return Result<User>.Fail(new NotFoundError { Message = "User not found" });

        return user;
    }
}
```

### Validation

Register a `FluentValidation` validator — failures are returned as `Result.Fail(ValidationError)` automatically. The handler is never invoked.

```csharp
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
```

### Events

Fire-and-forget events dispatched to all registered handlers. Each handler runs on its own background task with a fresh DI scope. Failures are isolated and logged — they never propagate to the caller and never stop other handlers.

```csharp
public class UserCreated : IEvent
{
    public int UserId { get; set; }
}

public class SendWelcomeEmailHandler : IEventHandler<UserCreated>
{
    public async Task Handle(UserCreated @event, CancellationToken cancellationToken)
    {
        await _emailService.SendWelcome(@event.UserId);
    }
}

// Publishing (returns immediately)
_mediator.Publish(new UserCreated { UserId = 42 });
```

> For durable (persistent) event dispatch backed by Hangfire, see
> [SolTechnology.Core.Hangfire](Hangfire.md).

### Result Pattern

```csharp
var result = await _mediator.Send(new GetUserQuery { UserId = 1 });

if (result.IsSuccess)
{
    var user = result.Data;
}
else
{
    var error = result.Error;
}

// Combinators
var output = await _mediator.Send(new GetUserQuery { UserId = 1 })
    .Map(user => user.Name)
    .Ensure(name => name.Length > 0, new Error { Message = "Empty name" });
```

For multi-step orchestration, see [Story.md](Story.md).

### Pipeline Behaviors

Custom behaviors can be registered to wrap every request:

```csharp
public class MyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        // before
        var response = await next();
        // after
        return response;
    }
}
```

Built-in behaviors (registered automatically):
- **LoggingPipelineBehavior** — tracks every request as a logical operation with structured logging and OpenTelemetry spans.
- **FluentValidationPipelineBehavior** — runs all validators; short-circuits with `ValidationError` on failure.

### Working with AI Agent

Building commands, queries, and events with an AI assistant (GitHub Copilot, Claude Code)? The
repository ships a **skill** — a narrow, file-cited procedure your agent can read on demand:

- [`command-query-event-story`](https://github.com/AdrianStrugala/SolTechnology.Core/blob/master/.github/skills/command-query-event-story/SKILL.md)
  — classify the artifact (command / query / event / Story), lay out the files one-folder-per-use-case,
  return `Result<T>`, wire `AddCQRS`, and add `[LogScope]` logging.

It points at the binding rules in the Coding Guide — keep these open while you write:

- [§3 — CQRS: commands, queries, validators, the `Result` pattern](https://github.com/AdrianStrugala/SolTechnology.Core/blob/master/docs/ClaudeCodingGuide.md)
- [§4 — Story framework for multi-step orchestration](https://github.com/AdrianStrugala/SolTechnology.Core/blob/master/docs/ClaudeCodingGuide.md)


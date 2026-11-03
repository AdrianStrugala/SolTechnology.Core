## SolTechnology.Core.CQRS

Send commands and queries through an in-house mediator and get a `Result` back, with automatic
FluentValidation, operation logging, and fire-and-forget events.

### Features

- `IMediator.Send` dispatches a command (`ICommand`, `ICommand<T>`) or a query (`IQuery<T>`) to its
  one handler; handlers return `Result` or `Result<T>`.
- FluentValidation validators run before the handler; a failure returns
  `Result.Fail(ValidationError)` and the handler is not called.
- Every request is logged as an operation (start, success, failed `Result`, exception) inside an
  OpenTelemetry activity; `[LogScope]` properties enter the log scope and `[Masked]` ones are masked.
- `IMediator.Publish` dispatches events fire-and-forget; a failing event handler is logged and never
  stops the others or the caller.
- Custom `IPipelineBehavior<TRequest, TResponse>` steps wrap every request in registration order.
- A request without a registered handler fails with an `InvalidOperationException` that names the
  request and the registration call to add.
- `IEventPublisher` is a replaceable seam for durable event dispatch, for example
  [SolTechnology.Core.Hangfire](Hangfire.md).

### Registration

Register the assemblies that contain your handlers, one handler family per call.

```csharp
using SolTechnology.Core.CQRS;

builder.Services.AddSolCQRS(o => o
    .RegisterCommandsFromAssembly(typeof(CreateUserCommand).Assembly)
    .RegisterQueriesFromAssembly(typeof(GetUserQuery).Assembly)
    .RegisterEventsFromAssembly(typeof(UserCreated).Assembly));
```

`AddSolCQRS` is idempotent, so every module can call it from its own `ModuleInstaller` with its own
assembly. Only public, non-abstract handler classes are discovered; sending a request without one
throws `InvalidOperationException` naming the request type and the `Register…FromAssembly` call to add.

### Configuration

| Option | Default | Purpose |
|---|---|---|
| `UseFluentValidation` | `true` | Registers `FluentValidationPipelineBehavior` and the validators in every registered assembly. |
| `UseLogging` | `true` | Registers `LoggingPipelineBehavior`. |

```csharp
builder.Services.AddSolCQRS(o =>
{
    o.UseFluentValidation = false;
    o.RegisterQueriesFromAssembly(typeof(GetUserQuery).Assembly);
});
```

### Usage

#### Commands

A command changes state; `ICommand` returns no data and `ICommand<T>` returns a `T`.

```csharp
public class CreateUserCommand : ICommand
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}

public class CreateUserCommandHandler(IUserRepository users) : ICommandHandler<CreateUserCommand>
{
    public async Task<Result> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        await users.Create(command.Name, command.Email, cancellationToken);
        return Result.Success();
    }
}

public class CreateOrderCommand : ICommand<int>
{
    public string Product { get; set; } = "";
}

public class CreateOrderCommandHandler(IOrderRepository orders) : ICommandHandler<CreateOrderCommand, int>
{
    public async Task<Result<int>> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        return await orders.Create(command.Product, cancellationToken);
    }
}
```

#### Queries

A query reads data and has no side effects.

```csharp
public class GetUserQuery : IQuery<User>
{
    public int UserId { get; set; }
}

public class GetUserQueryHandler(IUserRepository users) : IQueryHandler<GetUserQuery, User>
{
    public async Task<Result<User>> Handle(GetUserQuery query, CancellationToken cancellationToken)
    {
        var user = await users.GetById(query.UserId, cancellationToken);
        if (user is null)
        {
            return new NotFoundError { Message = $"User {query.UserId} not found" };
        }

        return user;
    }
}
```

#### Result pattern

`Send` returns the handler's `Result`; see [SolTechnology.Core](Core.md) for the `Result` and
`Error` types.

```csharp
public class UserService(IMediator mediator)
{
    public async Task<string?> FindName(int userId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetUserQuery { UserId = userId }, cancellationToken);
        return result.IsSuccess ? result.Data!.Name : null;
    }
}
```

For multi-step orchestration, promote the handler to a Tale; see [Tale.md](Tale.md).

#### Validation

Put an `AbstractValidator<T>` next to the command or query; it runs before the handler.

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

A failure returns `Result.Fail` with a `ValidationError` whose `Errors` groups the messages by
property name.

#### Events

`Publish` returns immediately; the handlers run later in the background.

```csharp
public class UserCreated : IEvent
{
    public int UserId { get; set; }
}

public class SendWelcomeEmailHandler(IEmailSender emails) : IEventHandler<UserCreated>
{
    public Task Handle(UserCreated @event, CancellationToken cancellationToken)
        => emails.SendWelcome(@event.UserId, cancellationToken);
}

public class UserOnboarding(IMediator mediator)
{
    public void Complete(int userId) => mediator.Publish(new UserCreated { UserId = userId });
}
```

| Aspect | Behaviour of the default in-memory publisher |
|---|---|
| Caller | `Publish` returns before any handler runs and never throws for a handler failure. |
| Scope | One background task and one new DI scope per published event. |
| Order | Handlers run one after another, in registration order. |
| Failures | A throwing handler is logged at Error with its exception; the next handler still runs. |
| Cancellation | Handlers get a token that is never cancelled; the publisher's caller cannot cancel them. |
| Durability | None: events in flight are lost when the process stops. Use [Hangfire](Hangfire.md) for durable dispatch. |

#### Pipeline behaviors

A behavior wraps every request; call `next()` to continue or return early to short-circuit.

```csharp
public class TimingBehavior<TRequest, TResponse>(ILogger<TimingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        var response = await next();
        logger.LogDebug("Request [{RequestType}] took [{ElapsedMs} ms]",
            typeof(TRequest).Name, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        return response;
    }
}
```

Behaviors run in registration order, the first registered outermost. `AddSolCQRS` registers
`LoggingPipelineBehavior` and then `FluentValidationPipelineBehavior`, so a behavior added after it
runs inside them:

```csharp
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TimingBehavior<,>));
```

#### Logging

`LoggingPipelineBehavior` writes these entries, with the request type name as `OperationName`:

| Outcome | EventId | Level | Template |
|---|---:|---|---|
| Start | 2137 | Information | `Operation: [{OperationName}]. Status: [START]` |
| Success | 2138 | Information | `Operation: [{OperationName}]. Status: [SUCCESS]. Duration: [{DurationMs} ms]` |
| Failed `Result` | 2149 | Warning | `Operation: [{OperationName}]. Status: [FAIL]. Duration: [{DurationMs} ms]. ErrorType: [{ErrorType}]. ErrorMessage: [{ErrorMessage}]` |
| Exception | 2139 | Error | `Operation: [{OperationName}]. Status: [FAIL]. Duration: [{DurationMs} ms]. Message: [{Message}]` |

A failed `Result` logs only the `Error` type name and `Error.Message`, never validation details,
inner errors, or data; the activity status becomes `Error`. Mark request properties to add them to
the scope of every entry and as activity tags:

```csharp
public class FindCityQuery : IQuery<string>
{
    [LogScope]
    public string CityName { get; set; } = "";

    [LogScope("UserEmail"), Masked(MaskMode.Partial, keepChars: 3)]
    public string Email { get; set; } = "";
}
```

Activities go to the `ActivitySource` named `CoreLoggingActivitySources.OperationsName`; see
[SolTechnology.Core.Logging](Log.md) for OpenTelemetry setup and masking.

### Testing

Send real requests through a `ServiceCollection` and assert with the `Result` helpers from
`SolTechnology.Core.Testing`.

```csharp
[Test]
public async Task Send_EmptyName_ReturnsValidationError()
{
    // Arrange
    var services = new ServiceCollection().AddLogging();
    services.AddSingleton<IUserRepository, InMemoryUserRepository>();
    services.AddSolCQRS(o => o.RegisterCommandsFromAssembly(typeof(CreateUserCommand).Assembly));
    await using var provider = services.BuildServiceProvider();
    await using var scope = provider.CreateAsyncScope();
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

    // Act
    var result = await mediator.Send(new CreateUserCommand { Name = "", Email = "jan@example.com" });

    // Assert
    result.ShouldBeFailure<ValidationError>().Errors.Should().ContainKey("Name");
}
```

### Conventions

- Return `Result.Fail` for business failures; throw only for bugs and infrastructure faults.
- Keep personal data out of `Error.Message`: a failed `Result` writes it to the logs.
- Pass the caller's `CancellationToken` to `Send` and on to every async call in the handler.
- Keep one folder per use case with the request, its validator, and its handler.
- Register handler assemblies in the owning module's `ModuleInstaller`, not in `Program.cs`.
- Inject `IMediator`; never resolve or call a handler directly.

### What ships in DI

| Service | Lifetime | Implementation |
|---|---|---|
| `IMediator` | Scoped | In-house mediator. |
| `IEventPublisher` | Singleton | In-memory fire-and-forget publisher (`TryAdd`, so a plugin can replace it). |
| `IEventDispatcher` | Scoped | Sequential fan-out to the event handlers of one event. |
| `IPipelineBehavior<,>` | Transient | `LoggingPipelineBehavior`, `FluentValidationPipelineBehavior` (each when enabled). |
| `ICommandHandler<>`, `ICommandHandler<,>`, `IQueryHandler<,>`, `IEventHandler<>` | Transient | Every public handler class in the registered assemblies. |
| `IValidator<T>` | Scoped | Every validator in the registered assemblies (when `UseFluentValidation`). |

### Working with AI Agent

Building commands, queries, and events with an AI assistant (GitHub Copilot, Claude Code)? The
repository ships a **skill** — a narrow, file-cited procedure your agent can read on demand:

- [`command-query-event-tale`](https://github.com/AdrianStrugala/SolTechnology.Core/blob/master/.github/skills/command-query-event-tale/SKILL.md)
  — classify the artifact (command / query / event / Tale), lay out the files one-folder-per-use-case,
  return `Result<T>`, wire `AddSolCQRS`, and add `[LogScope]` logging.

It points at the binding rules in the Coding Guide — keep these open while you write:

- [§3 — CQRS: commands, queries, validators, the `Result` pattern](https://github.com/AdrianStrugala/SolTechnology.Core/blob/master/docs/ClaudeCodingGuide.md)
- [§4 — Tale framework for multi-step orchestration](https://github.com/AdrianStrugala/SolTechnology.Core/blob/master/docs/ClaudeCodingGuide.md)

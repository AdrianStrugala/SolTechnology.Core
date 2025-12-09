### Overview

The SolTechnology.Core.CQRS library provides a complete CQRS (Command Query Responsibility Segregation) implementation built on MediatR. It includes the Result pattern for explicit success/failure handling, Chain handlers for complex multi-step operations, and automatic validation and logging through pipeline behaviors.

### Registration

For installing the library, reference **SolTechnology.Core.CQRS** nuget package and invoke the appropriate registration methods:

```csharp
// Register command handlers
services.RegisterCommands();

// Register query handlers
services.RegisterQueries();

// Register chain steps (for complex operations)
services.RegisterChain();
```

### Configuration

No configuration is needed. The registration methods automatically:
- Register all command and query handlers from the calling assembly
- Configure FluentValidation for input validation
- Add logging and validation pipeline behaviors

### Usage

1) Define a Query and Handler

```csharp
public class GetUserQuery : IRequest<Result<User>>
{
    public int UserId { get; set; }
}

public class GetUserQueryHandler : IQueryHandler<GetUserQuery, User>
{
    public async Task<Result<User>> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetById(request.UserId);

        if (user == null)
            return Result<User>.Fail("User not found");

        return Result<User>.Success(user);
    }
}
```

2) Define a Command and Handler

```csharp
public class CreateUserCommand : IRequest<Result>
{
    public string Name { get; set; }
    public string Email { get; set; }
}

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand>
{
    public async Task<Result> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        await _userRepository.Create(new User
        {
            Name = request.Name,
            Email = request.Email
        });

        return Result.Success();
    }
}
```

3) Add Validation (Optional)

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

4) Use Chain Pattern for Complex Operations

```csharp
public class ComplexOperationContext : ChainContext<MyInput, MyOutput>
{
    public string IntermediateData { get; set; }
}

public class Step1 : IChainStep<ComplexOperationContext>
{
    public async Task<Result> Execute(ComplexOperationContext context)
    {
        // Step 1 logic
        context.IntermediateData = "processed";
        return Result.Success();
    }
}

public class ComplexOperationHandler : ChainHandler<MyInput, ComplexOperationContext, MyOutput>
{
    protected override async Task HandleChain()
    {
        await Invoke<Step1>();
        await Invoke<Step2>();
        await Invoke<Step3>();
    }
}
```

5) Result Pattern Usage

```csharp
var result = await _mediator.Send(new GetUserQuery { UserId = 1 });

if (result.IsSuccess)
{
    var user = result.Value;
    // Use the user
}
else
{
    var error = result.Error;
    // Handle the error
}
```

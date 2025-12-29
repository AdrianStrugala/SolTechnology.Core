# SolTechnology.Core.Story

A powerful workflow orchestration framework for building complex, multi-step business processes with support for persistence, pause/resume, and interactive chapters.

## Overview

The Story Framework provides a narrative-driven approach to orchestrating complex workflows. It replaces the older Chain and Flow frameworks with a unified, more expressive API that reads like a table of contents.

### Key Features

- **Narrative Structure** - Workflows read like stories with chapters
- **Type-Safe Context** - Strongly-typed narration flows through all chapters
- **Interactive Chapters** - Pause workflows to collect user input
- **Persistence** - Save and resume stories across application restarts
- **Error Handling** - Comprehensive error aggregation and reporting
- **DI Integration** - First-class dependency injection support
- **CQRS Compatible** - Works seamlessly with CQRS handlers

## Installation

```bash
dotnet add package SolTechnology.Core.Story
```

## Quick Start

### 1. Define Your Story Models

```csharp
// Input - what starts the story
public class SaveCityInput
{
    public string CityName { get; set; }
    public string CountryCode { get; set; }
}

// Narration - the context that flows through chapters
public class SaveCityNarration : Narration<SaveCityInput, SaveCityResult>
{
    public City? ExistingCity { get; set; }
    public string? AlternativeName { get; set; }
    public int SearchCount { get; set; }
}

// Output - what the story returns
public class SaveCityResult
{
    public string CityId { get; set; }
    public bool IsNew { get; set; }
}
```

### 2. Create Chapters

```csharp
// Regular chapter - executes automatically
public class LoadExistingCity : Chapter<SaveCityNarration>
{
    private readonly ICityRepository _repository;

    public LoadExistingCity(ICityRepository repository)
    {
        _repository = repository;
    }

    public override async Task<Result> Read(SaveCityNarration narration)
    {
        narration.ExistingCity = await _repository.FindByName(narration.Input.CityName);
        return Result.Success();
    }
}

// Another chapter in the sequence
public class AssignAlternativeName : Chapter<SaveCityNarration>
{
    public override Task<Result> Read(SaveCityNarration narration)
    {
        if (narration.ExistingCity == null)
        {
            narration.AlternativeName = GenerateAlternativeName(narration.Input.CityName);
        }
        return Result.SuccessAsTask();
    }

    private string GenerateAlternativeName(string cityName) => $"{cityName}-Alt";
}
```

### 3. Write Your Story

```csharp
public class SaveCityStory : StoryHandler<SaveCityInput, SaveCityNarration, SaveCityResult>
{
    public SaveCityStory(
        IServiceProvider serviceProvider,
        ILogger<SaveCityStory> logger)
        : base(serviceProvider, logger)
    {
    }

    protected override async Task TellStory()
    {
        await ReadChapter<LoadExistingCity>();
        await ReadChapter<AssignAlternativeName>();
        await ReadChapter<IncrementSearchCount>();
        await ReadChapter<SaveToDatabase>();
    }
}
```

### 4. Register and Execute

```csharp
// In Startup/Program.cs
services.RegisterStories();

// Execute the story
var story = serviceProvider.GetRequiredService<SaveCityStory>();
var input = new SaveCityInput { CityName = "Paris", CountryCode = "FR" };
var result = await story.Handle(input);

if (result.IsSuccess)
{
    Console.WriteLine($"City saved: {result.Data.CityId}");
}
```

## Core Concepts

### StoryHandler

The orchestrator that defines the sequence of chapters. It's the "director" of your workflow.

```csharp
public abstract class StoryHandler<TInput, TNarration, TOutput>
    where TInput : class
    where TNarration : Narration<TInput, TOutput>, new()
    where TOutput : class, new()
```

**Key Methods:**
- `TellStory()` - Define your chapter sequence
- `ReadChapter<TChapter>()` - Execute a chapter
- `Handle(TInput input)` - Entry point to run the story

### Narration

The context object that flows through all chapters, carrying state and intermediate data.

```csharp
public abstract class Narration<TInput, TOutput>
    where TInput : class
    where TOutput : class, new()
{
    public TInput Input { get; set; }
    public TOutput Output { get; set; }
}
```

**Design Pattern:**
- Add properties for intermediate data needed by chapters
- Initialize `Output` properties in the final chapter
- Read-only access to `Input` throughout the story

### Chapter

A single step in your story that performs a specific task.

```csharp
public abstract class Chapter<TNarration> : IChapter<TNarration>
    where TNarration : class
{
    public abstract Task<Result> Read(TNarration narration);
}
```

**Best Practices:**
- Keep chapters focused on a single responsibility
- Use dependency injection for services
- Return `Result.Success()` or `Result.Fail("error message")`
- Chapters are resolved from DI, so they can have dependencies

### InteractiveChapter

A chapter that pauses the story to collect user input.

```csharp
public abstract class InteractiveChapter<TNarration, TChapterInput> : IChapter<TNarration>
    where TNarration : class
{
    // Define what data is required
    public abstract List<DataField> GetRequiredInputSchema();

    // Execute when input is provided
    public abstract Task<Result> ReadWithInput(TNarration narration, TChapterInput userInput);
}
```

**Example:**

```csharp
public class RequestCustomerDetails : InteractiveChapter<OrderNarration, CustomerDetails>
{
    public override List<DataField> GetRequiredInputSchema()
    {
        return new List<DataField>
        {
            new() { Name = "Name", Type = "string", Required = true },
            new() { Name = "Email", Type = "string", Required = true },
            new() { Name = "Address", Type = "string", Required = false }
        };
    }

    public override Task<Result> ReadWithInput(OrderNarration narration, CustomerDetails userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput.Name))
        {
            return Result.FailAsTask("Customer name is required");
        }

        narration.CustomerName = userInput.Name;
        narration.CustomerEmail = userInput.Email;

        return Result.SuccessAsTask();
    }
}
```

## Advanced Features

### Persistence & Pause/Resume

Stories can be paused at interactive chapters and resumed later, even after application restart.

#### 1. Enable Persistence

```csharp
// In-Memory (for testing)
var options = StoryOptions.WithInMemoryPersistence();

// SQLite (for production)
var options = StoryOptions.WithSqlitePersistence("path/to/stories.db");

// Custom repository
var options = new StoryOptions
{
    EnablePersistence = true,
    Repository = new MyCustomRepository()
};
```

#### 2. Create Story with Persistence

```csharp
public class OrderProcessingStory : StoryHandler<OrderInput, OrderNarration, OrderOutput>
{
    public OrderProcessingStory(
        IServiceProvider serviceProvider,
        ILogger<OrderProcessingStory> logger,
        StoryOptions options) // Accept options
        : base(serviceProvider, logger, options)
    {
    }

    protected override async Task TellStory()
    {
        await ReadChapter<ValidateOrder>();
        await ReadChapter<RequestCustomerDetails>(); // Interactive - will pause here
        await ReadChapter<ProcessPayment>();
        await ReadChapter<SendConfirmation>();
    }
}
```

#### 3. Use StoryManager for Orchestration

```csharp
// Start a story
var manager = serviceProvider.GetRequiredService<StoryManager>();
var startResult = await manager.StartStory<OrderProcessingStory, OrderInput, OrderNarration, OrderOutput>(input);

if (startResult.IsFailure && startResult.Error.Message.Contains("paused"))
{
    var storyId = startResult.Data!.StoryId;
    Console.WriteLine($"Story paused at: {startResult.Data.CurrentChapter?.ChapterId}");

    // Later, resume with user input
    var customerDetails = new CustomerDetails
    {
        Name = "John Doe",
        Email = "john@example.com"
    };
    var userInput = JsonSerializer.SerializeToElement(customerDetails);

    var resumeResult = await manager.ResumeStory<OrderProcessingStory, OrderInput, OrderNarration, OrderOutput>(
        storyId,
        userInput);

    if (resumeResult.IsSuccess)
    {
        Console.WriteLine("Story completed successfully");
    }
}
```

#### 4. Query Story State

```csharp
var storyState = await manager.GetStoryState(storyId);

if (storyState.IsSuccess)
{
    var instance = storyState.Data;
    Console.WriteLine($"Status: {instance.Status}");
    Console.WriteLine($"Current Chapter: {instance.CurrentChapter?.ChapterId}");
    Console.WriteLine($"History: {instance.History.Count} chapters executed");

    // Check required input schema
    if (instance.CurrentChapter?.RequiredData != null)
    {
        foreach (var field in instance.CurrentChapter.RequiredData)
        {
            Console.WriteLine($"  - {field.Name} ({field.Type}): {(field.Required ? "Required" : "Optional")}");
        }
    }
}
```

### Story Status Lifecycle

```
Created → Running → WaitingForInput ⟲
                ↓
           Completed / Failed / Cancelled
```

- **Created**: Story instance created but not started
- **Running**: Currently executing chapters
- **WaitingForInput**: Paused at an interactive chapter
- **Completed**: All chapters executed successfully
- **Failed**: A chapter returned an error
- **Cancelled**: Execution was cancelled via CancellationToken

### Error Handling

Stories automatically collect and aggregate errors from all chapters.

```csharp
var result = await story.Handle(input);

if (result.IsFailure)
{
    if (result.Error is AggregateError aggregateError)
    {
        // Multiple chapters failed
        foreach (var error in aggregateError.Errors)
        {
            Console.WriteLine($"Error: {error.Message}");
        }
    }
    else
    {
        // Single error
        Console.WriteLine($"Error: {result.Error.Message}");
    }
}
```

### Stop on First Error

By default, stories continue executing chapters even after failures. To stop on first error:

```csharp
var options = new StoryOptions
{
    StopOnFirstError = true
};
```

## Integration Patterns

### CQRS Integration

Stories work seamlessly as Query or Command handlers:

```csharp
public class CalculateBestPathHandler :
    StoryHandler<CalculateBestPathQuery, CalculateBestPathNarration, CalculateBestPathResult>,
    IQueryHandler<CalculateBestPathQuery, CalculateBestPathResult>
{
    public CalculateBestPathHandler(
        IServiceProvider serviceProvider,
        ILogger<CalculateBestPathHandler> logger)
        : base(serviceProvider, logger)
    {
    }

    protected override async Task TellStory()
    {
        await ReadChapter<InitiateContext>();
        await ReadChapter<DownloadRoadData>();
        await ReadChapter<FindProfitablePath>();
        await ReadChapter<SolveTsp>();
        await ReadChapter<FormResult>();
    }
}

// Register as both Story and Query
services.RegisterStories();
services.RegisterQueries();

// Use through MediatR
var result = await mediator.Send(new CalculateBestPathQuery { ... });
```

### Domain Services

Use stories to implement complex domain services:

```csharp
public class SaveCityDomainService :
    StoryHandler<SaveCityInput, SaveCityNarration, SaveCityResult>
{
    protected override async Task TellStory()
    {
        await ReadChapter<LoadCity>();
        await ReadChapter<GenerateAlternativeName>();
        await ReadChapter<IncrementSearchCount>();
        await ReadChapter<SaveCity>();
    }
}
```

## Repository Implementations

### In-Memory Repository

For testing and development:

```csharp
var options = StoryOptions.WithInMemoryPersistence();
```

- Stories stored in memory
- Lost on application restart
- Thread-safe with proper locking

### SQLite Repository

For production persistence:

```csharp
var options = StoryOptions.WithSqlitePersistence("stories.db");
// or
var options = StoryOptions.WithSqlitePersistence(); // Uses default path
```

- Stories persisted to SQLite database
- Survives application restarts
- Default path: `%LocalAppData%/SolTechnology/StoryFramework/stories.db`
- Thread-safe with connection pooling

### Custom Repository

Implement your own repository:

```csharp
public class CosmosDbStoryRepository : IStoryRepository
{
    public Task<StoryInstance?> FindById(string storyId) { ... }
    public Task SaveAsync(StoryInstance storyInstance) { ... }
    public Task DeleteAsync(string storyId) { ... }
}

// Use it
var options = new StoryOptions
{
    EnablePersistence = true,
    Repository = new CosmosDbStoryRepository(cosmosClient)
};
```

## Registration

### Automatic Registration

Scans the calling assembly for all stories and chapters:

```csharp
services.RegisterStories();
```

This registers:
- All `StoryHandler<,,>` implementations
- All `Chapter<>` implementations
- All `InteractiveChapter<,>` implementations
- `StoryManager` for orchestration
- Default `InMemoryStoryRepository`

### With Persistence

```csharp
// In-memory
services.RegisterStories(StoryOptions.WithInMemoryPersistence());

// SQLite
services.RegisterStories(StoryOptions.WithSqlitePersistence("stories.db"));

// Custom
services.RegisterStories(new StoryOptions
{
    EnablePersistence = true,
    Repository = new MyRepository(),
    StopOnFirstError = true
});
```

### Manual Registration

For more control:

```csharp
services.AddTransient<SaveCityStory>();
services.AddTransient<LoadExistingCity>();
services.AddTransient<AssignAlternativeName>();
// ... register each chapter individually
```

## Testing Stories

### Testing Individual Chapters

```csharp
[Test]
public async Task LoadExistingCity_CityExists_LoadsCity()
{
    // Arrange
    var repository = new Mock<ICityRepository>();
    repository.Setup(r => r.FindByName("Paris"))
        .ReturnsAsync(new City { Id = "1", Name = "Paris" });

    var chapter = new LoadExistingCity(repository.Object);
    var narration = new SaveCityNarration
    {
        Input = new SaveCityInput { CityName = "Paris" }
    };

    // Act
    var result = await chapter.Read(narration);

    // Assert
    result.IsSuccess.Should().BeTrue();
    narration.ExistingCity.Should().NotBeNull();
    narration.ExistingCity!.Name.Should().Be("Paris");
}
```

### Testing Complete Stories

```csharp
[Test]
public async Task SaveCityStory_NewCity_SavesSuccessfully()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();
    services.RegisterStories();
    services.AddTransient<ICityRepository, InMemoryCityRepository>();

    var serviceProvider = services.BuildServiceProvider();
    var story = serviceProvider.GetRequiredService<SaveCityStory>();

    // Act
    var input = new SaveCityInput { CityName = "Paris", CountryCode = "FR" };
    var result = await story.Handle(input);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Data.IsNew.Should().BeTrue();
}
```

### Testing Pause/Resume

```csharp
[Test]
public async Task OrderStory_PausesAndResumes_WithUserInput()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();
    services.RegisterStories(StoryOptions.WithInMemoryPersistence());
    var serviceProvider = services.BuildServiceProvider();
    var manager = serviceProvider.GetRequiredService<StoryManager>();

    // Act - Start story
    var input = new OrderInput { OrderId = "ORD-001" };
    var startResult = await manager.StartStory<OrderProcessingStory, OrderInput, OrderNarration, OrderOutput>(input);

    // Assert - Should pause
    startResult.IsFailure.Should().BeTrue();
    startResult.Error!.Message.Should().Contain("paused");
    var storyId = startResult.Data!.StoryId;

    // Act - Resume with input
    var customerDetails = new CustomerDetails { Name = "John", Email = "john@example.com" };
    var userInput = JsonSerializer.SerializeToElement(customerDetails);
    var resumeResult = await manager.ResumeStory<OrderProcessingStory, OrderInput, OrderNarration, OrderOutput>(
        storyId,
        userInput);

    // Assert - Should complete
    resumeResult.IsSuccess.Should().BeTrue();
    resumeResult.Data!.Status.Should().Be(StoryStatus.Completed);
}
```

## Best Practices

### 1. Keep Chapters Focused

Each chapter should have a single, clear responsibility:

```csharp
// ✅ Good - focused responsibility
public class LoadExistingCity : Chapter<SaveCityNarration>
{
    public override async Task<Result> Read(SaveCityNarration narration)
    {
        narration.ExistingCity = await _repository.FindByName(narration.Input.CityName);
        return Result.Success();
    }
}

// ❌ Bad - doing too much
public class LoadAndSaveCity : Chapter<SaveCityNarration>
{
    public override async Task<Result> Read(SaveCityNarration narration)
    {
        var city = await _repository.FindByName(narration.Input.CityName);
        if (city == null)
        {
            city = new City { Name = narration.Input.CityName };
            await _repository.Save(city);
        }
        narration.ExistingCity = city;
        return Result.Success();
    }
}
```

### 2. Use Meaningful Names

Chapter names should clearly describe what they do:

```csharp
// ✅ Good
await ReadChapter<ValidateOrderDetails>();
await ReadChapter<CalculateShippingCost>();
await ReadChapter<ApplyDiscountCodes>();
await ReadChapter<ProcessPayment>();

// ❌ Bad
await ReadChapter<Step1>();
await ReadChapter<Step2>();
await ReadChapter<ProcessStuff>();
```

### 3. Design Your Narration

Think carefully about what data flows through your story:

```csharp
// ✅ Good - clear, organized narration
public class OrderNarration : Narration<OrderInput, OrderOutput>
{
    // Loaded data
    public Customer? Customer { get; set; }
    public List<Product> Products { get; set; } = new();

    // Calculated values
    public decimal SubTotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal TotalCost { get; set; }

    // Validation results
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
}
```

### 4. Handle Errors Gracefully

Always validate inputs and return meaningful errors:

```csharp
public override Task<Result> Read(SaveCityNarration narration)
{
    if (string.IsNullOrWhiteSpace(narration.Input.CityName))
    {
        return Result.FailAsTask("City name is required");
    }

    if (narration.Input.CityName.Length > 100)
    {
        return Result.FailAsTask("City name must be less than 100 characters");
    }

    // Process...
    return Result.SuccessAsTask();
}
```

### 5. Use Dependency Injection

Chapters should declare their dependencies in the constructor:

```csharp
public class LoadExistingCity : Chapter<SaveCityNarration>
{
    private readonly ICityRepository _repository;
    private readonly ILogger<LoadExistingCity> _logger;
    private readonly IDistributedCache _cache;

    public LoadExistingCity(
        ICityRepository repository,
        ILogger<LoadExistingCity> logger,
        IDistributedCache cache)
    {
        _repository = repository;
        _logger = logger;
        _cache = cache;
    }

    public override async Task<Result> Read(SaveCityNarration narration)
    {
        _logger.LogInformation("Loading city: {CityName}", narration.Input.CityName);
        // ...
    }
}
```

### 6. Interactive Chapter Design

Keep interactive chapters simple and validation-focused:

```csharp
public class RequestCustomerDetails : InteractiveChapter<OrderNarration, CustomerDetails>
{
    public override List<DataField> GetRequiredInputSchema()
    {
        // Clearly define what you need
        return new List<DataField>
        {
            new() { Name = "Name", Type = "string", Required = true, Description = "Customer full name" },
            new() { Name = "Email", Type = "email", Required = true, Description = "Contact email address" },
            new() { Name = "Phone", Type = "string", Required = false, Description = "Phone number (optional)" }
        };
    }

    public override Task<Result> ReadWithInput(OrderNarration narration, CustomerDetails input)
    {
        // Validate thoroughly
        if (string.IsNullOrWhiteSpace(input.Name))
            return Result.FailAsTask("Customer name is required");

        if (!IsValidEmail(input.Email))
            return Result.FailAsTask("Invalid email address");

        // Store in narration
        narration.CustomerName = input.Name;
        narration.CustomerEmail = input.Email;
        narration.CustomerPhone = input.Phone;

        return Result.SuccessAsTask();
    }
}
```

## Migration from Chain/Flow

If you're migrating from the older Chain or Flow frameworks:

### Chain → Story

```csharp
// Old Chain
public class MyHandler : ChainHandler<MyInput, MyContext, MyOutput>
{
    protected override async Task HandleChain()
    {
        await Invoke<Step1>();
        await Invoke<Step2>();
    }
}

// New Story
public class MyStory : StoryHandler<MyInput, MyNarration, MyOutput>
{
    protected override async Task TellStory()
    {
        await ReadChapter<Step1>();
        await ReadChapter<Step2>();
    }
}
```

**Changes:**
- `ChainHandler` → `StoryHandler`
- `ChainContext` → `Narration`
- `Invoke<T>()` → `ReadChapter<T>()`
- `HandleChain()` → `TellStory()`
- `IChainStep` → `Chapter`

### Flow → Story

```csharp
// Old Flow
public class MyWorkflow : FlowHandler<MyInput, MyState, MyOutput>
{
    protected override async Task ExecuteFlow()
    {
        await RunNode<Node1>();
        await RunNode<Node2>();
    }
}

// New Story
public class MyStory : StoryHandler<MyInput, MyNarration, MyOutput>
{
    protected override async Task TellStory()
    {
        await ReadChapter<Chapter1>();
        await ReadChapter<Chapter2>();
    }
}
```

**Changes:**
- `FlowHandler` → `StoryHandler`
- `FlowState` → `Narration`
- `RunNode<T>()` → `ReadChapter<T>()`
- `ExecuteFlow()` → `TellStory()`
- `IFlowNode` → `Chapter`

## Troubleshooting

### "Chapter not registered in DI container"

**Problem**: Chapter isn't found when executing story.

**Solution**:
```csharp
// Make sure you called RegisterStories()
services.RegisterStories();

// Or register the chapter manually
services.AddTransient<MyChapter>();
```

### "Failed to deserialize story context"

**Problem**: Narration can't be deserialized during resume.

**Solution**: Ensure your Narration class and all its properties are JSON-serializable:
```csharp
public class MyNarration : Narration<MyInput, MyOutput>
{
    // ✅ Good - simple types
    public string Name { get; set; }
    public int Count { get; set; }
    public List<string> Items { get; set; } = new();

    // ❌ Bad - complex types without converters
    public MyComplexType ComplexData { get; set; } // Won't serialize well
}
```

### Story doesn't pause at interactive chapter

**Problem**: Interactive chapter executes without waiting for input.

**Solution**: Make sure you're using `StoryManager` to start the story:
```csharp
// ❌ Wrong - direct execution
var story = serviceProvider.GetRequiredService<MyStory>();
await story.Handle(input); // Won't pause properly

// ✅ Correct - use StoryManager
var manager = serviceProvider.GetRequiredService<StoryManager>();
await manager.StartStory<MyStory, MyInput, MyNarration, MyOutput>(input);
```

## Performance Considerations

### Chapter Execution

- Chapters are resolved from DI for each execution
- Use transient lifetime for stateless chapters
- Use scoped lifetime if chapters need to share state within a request

### Persistence

- In-memory repository: Fast but lost on restart
- SQLite repository: Slower but persistent
- Custom repository: Performance depends on implementation

### Large Narrations

If your narration becomes very large:
- Consider breaking into multiple smaller stories
- Store large data externally and keep references in narration
- Use compression for serialized context

## API Reference

### Core Classes

#### StoryHandler<TInput, TNarration, TOutput>

```csharp
public abstract class StoryHandler<TInput, TNarration, TOutput>
    where TInput : class
    where TNarration : Narration<TInput, TOutput>, new()
    where TOutput : class, new()
{
    protected StoryHandler(IServiceProvider serviceProvider, ILogger logger);
    protected StoryHandler(IServiceProvider serviceProvider, ILogger logger, StoryOptions? options);

    protected abstract Task TellStory();
    protected Task ReadChapter<TChapter>() where TChapter : IChapter<TNarration>;

    public virtual Task<Result<TOutput>> Handle(TInput input, CancellationToken cancellationToken = default);

    public TNarration Narration { get; set; }
}
```

#### Narration<TInput, TOutput>

```csharp
public abstract class Narration<TInput, TOutput>
    where TInput : class
    where TOutput : class, new()
{
    public TInput Input { get; set; }
    public TOutput Output { get; set; }
    public string? StoryInstanceId { get; internal set; }
    public string? CurrentChapterId { get; internal set; }
}
```

#### Chapter<TNarration>

```csharp
public abstract class Chapter<TNarration> : IChapter<TNarration>
    where TNarration : class
{
    public virtual string ChapterId { get; }
    public abstract Task<Result> Read(TNarration narration);
}
```

#### InteractiveChapter<TNarration, TChapterInput>

```csharp
public abstract class InteractiveChapter<TNarration, TChapterInput> : IChapter<TNarration>
    where TNarration : class
{
    public virtual string ChapterId { get; }
    public abstract List<DataField> GetRequiredInputSchema();
    public abstract Task<Result> ReadWithInput(TNarration narration, TChapterInput userInput);

    Task<Result> IChapter<TNarration>.Read(TNarration narration);
    public Task<Result> ReadWithInput(TNarration narration, TChapterInput userInput);
}
```

#### StoryManager

```csharp
public class StoryManager
{
    public StoryManager(IServiceProvider serviceProvider, IStoryRepository repository, ILogger<StoryManager> logger);

    public Task<Result<StoryInstance>> StartStory<THandler, TInput, TNarration, TOutput>(TInput input)
        where THandler : StoryHandler<TInput, TNarration, TOutput>
        where TInput : class
        where TNarration : Narration<TInput, TOutput>, new()
        where TOutput : class, new();

    public Task<Result<StoryInstance>> ResumeStory<THandler, TInput, TNarration, TOutput>(
        string storyId,
        JsonElement? userInput = null)
        where THandler : StoryHandler<TInput, TNarration, TOutput>
        where TInput : class
        where TNarration : Narration<TInput, TOutput>, new()
        where TOutput : class, new();

    public Task<Result<StoryInstance>> GetStoryState(string storyId);
}
```

#### StoryOptions

```csharp
public class StoryOptions
{
    public bool EnablePersistence { get; set; }
    public IStoryRepository? Repository { get; set; }
    public bool StopOnFirstError { get; set; }

    public static StoryOptions Default { get; }
    public static StoryOptions WithInMemoryPersistence();
    public static StoryOptions WithSqlitePersistence(string? dbPath = null);
}
```

### Extension Methods

```csharp
public static class ModuleInstaller
{
    public static IServiceCollection RegisterStories(
        this IServiceCollection services,
        StoryOptions? options = null);
}
```

## License

This library is part of the SolTechnology.Core framework.

## Support

For issues, questions, or contributions, please refer to the main SolTechnology.Core repository.

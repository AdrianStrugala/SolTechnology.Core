### Overview

The SolTechnology.Core.Story library provides a unified workflow orchestration framework for multi-step business processes. It supports both simple automated workflows and pausable interactive workflows with SQLite persistence. Built on the Tale Code philosophy, Story Framework makes complex workflows read like well-written prose.

### Installation

Install the **SolTechnology.Core.Story** NuGet package:

```bash
dotnet add package SolTechnology.Core.Story
```

### Registration

```csharp
// Basic registration (automated workflows only)
services.AddStoryFramework();

// With persistence (for pausable interactive workflows)
services.AddStoryFramework(options =>
{
    options.EnablePersistence = true;
    options.DatabasePath = "stories.db";
});

// For testing - in-memory persistence
services.AddSingleton(StoryOptions.WithInMemoryPersistence());
```

### Configuration

No additional configuration needed. The registration automatically:
- Registers all StoryHandlers and Chapters from calling assembly
- Configures StoryEngine for orchestration
- Sets up SQLite persistence (if enabled)
- Integrates with CQRS Result pattern

### Usage

#### 1) Basic Automated Story

```csharp
// Define input, output, and context
public class OrderInput
{
    public int OrderId { get; set; }
}

public class OrderOutput
{
    public string Status { get; set; }
}

public class OrderNarration : Context<OrderInput, OrderOutput>
{
    public string CustomerEmail { get; set; }
    public decimal TotalAmount { get; set; }
}

// Define story handler
public class ProcessOrderStory : StoryHandler<OrderInput, Ordercontext, OrderOutput>
{
    public ProcessOrderStory(IServiceProvider sp, ILogger<ProcessOrderStory> logger)
        : base(sp, logger) { }

    protected override async Task TellStory()
    {
        await ReadChapter<ValidateOrderChapter>();
        await ReadChapter<ProcessPaymentChapter>();
        await ReadChapter<SendConfirmationChapter>();

        context.Output.Status = "Completed";
    }
}

// Define chapters
public class ValidateOrderChapter : Chapter<OrderNarration>
{
    public override async Task<Result> Read(OrderNarration context)
    {
        // Validation logic
        if (context.Input.OrderId <= 0)
            return Result.Fail("Invalid order ID");

        return Result.Success();
    }
}
```

#### 2) Interactive Story with Pause/Resume

```csharp
// Define interactive chapter
public class CollectPaymentInfoChapter : InteractiveChapter<Ordercontext, PaymentInfo>
{
    public override Task<Result> ReadWithInput(OrderNarration context, PaymentInfo userInput)
    {
        // Validate user input
        if (string.IsNullOrWhiteSpace(userInput.CardNumber))
            return Result.FailAsTask("Card number is required");

        if (userInput.CardNumber.Length != 16)
            return Result.FailAsTask("Invalid card number");

        // Process input
        context.PaymentMethod = userInput.CardNumber;
        return Result.SuccessAsTask();
    }
}

public class PaymentInfo
{
    public string CardNumber { get; set; }
    public string Cvv { get; set; }
}

// Story with interactive chapter
public class CheckoutStory : StoryHandler<OrderInput, Ordercontext, OrderOutput>
{
    protected override async Task TellStory()
    {
        await ReadChapter<ValidateCartChapter>();
        await ReadChapter<CollectPaymentInfoChapter>();  // Pauses here for user input
        await ReadChapter<ProcessPaymentChapter>();
        await ReadChapter<SendConfirmationChapter>();

        context.Output.Status = "Completed";
    }
}
```

#### 3) Using StoryManager for Pause/Resume

```csharp
// Start a story
var input = new OrderInput { OrderId = 123 };
var result = await storyManager.StartStory<CheckoutStory, OrderInput, Ordercontext, OrderOutput>(input);

if (result.IsSuccess)
{
    var storyInstance = result.Data;

    if (storyInstance.Status == StoryStatus.WaitingForInput)
    {
        // Story paused at interactive chapter
        var storyId = storyInstance.StoryId;
        var requiredData = storyInstance.CurrentChapter.RequiredData;

        // ... collect user input ...

        // Resume with user input
        var userInput = JsonDocument.Parse("{\"cardNumber\": \"1234567812345678\", \"cvv\": \"123\"}");
        var resumeResult = await storyManager.ResumeStory<CheckoutStory, OrderInput, Ordercontext, OrderOutput>(
            storyId,
            userInput.RootElement);

        if (resumeResult.IsSuccess)
        {
            // Story completed or paused again
        }
    }
}
```

#### 4) Direct Handler Usage (for simple workflows)

```csharp
// Inject handler directly
public class OrderController : ControllerBase
{
    private readonly ProcessOrderStory _story;

    public OrderController(ProcessOrderStory story)
    {
        _story = story;
    }

    [HttpPost]
    public async Task<IActionResult> ProcessOrder([FromBody] OrderInput input)
    {
        var result = await _story.Handle(input);

        if (result.IsSuccess)
            return Ok(result.Value);

        return BadRequest(result.Error);
    }
}
```

### Key Features

- **Tale Code Philosophy**: Workflows read like well-written stories with `TellStory()`, chapters, and context
- **Automated Workflows**: Simple linear workflows without user interaction
- **Interactive Workflows**: Pausable workflows with user input validation
- **SQLite Persistence**: Save and resume workflow state across restarts
- **Result Pattern**: Explicit success/failure handling integrated with CQRS
- **Chapter Validation**: Built-in input validation for interactive chapters
- **History Tracking**: Full audit trail of chapter execution
- **Cancellation Support**: Graceful cancellation with `CancellationToken`
- **Error Aggregation**: Collect multiple errors during workflow execution
- **DI Integration**: Full dependency injection support for chapters

### API Controller Integration

Story Framework includes a base `StoryController` for HTTP APIs:

```csharp
public class OrderStoryController : StoryController
{
    public OrderStoryController(StoryManager manager) : base(manager) { }
}

// Endpoints automatically available:
// POST /api/story/start/{storyType} - Start a new story
// POST /api/story/resume/{storyId} - Resume paused story
// GET /api/story/{storyId} - Get story state
```

### Tale Code Example

```csharp
// Traditional approach
var step1Result = await ValidateOrder(order);
if (!step1Result.IsSuccess) return step1Result.Error;
var step2Result = await ProcessPayment(order);
if (!step2Result.IsSuccess) return step2Result.Error;
var step3Result = await SendConfirmation(order);
if (!step3Result.IsSuccess) return step3Result.Error;
return Result.Success();

// Story Framework approach - reads like a tale
protected override async Task TellStory()
{
    await ReadChapter<ValidateOrderChapter>();
    await ReadChapter<ProcessPaymentChapter>();
    await ReadChapter<SendConfirmationChapter>();

    context.Output.Status = "Order processed successfully";
}
```

### Documentation

- [Story Implementation Plan](https://github.com/AdrianStrugala/SolTechnology.Core/blob/master/docs/Story-Implementation-Plan.md) - Architecture decisions and migration guide
- [Tale Code Philosophy](https://github.com/AdrianStrugala/SolTechnology.Core/blob/master/docs/Tale.md) - Making code readable like prose
- [GitHub Repository](https://github.com/AdrianStrugala/SolTechnology.Core)

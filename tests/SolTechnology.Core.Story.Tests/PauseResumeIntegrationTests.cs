using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Story;
using SolTechnology.Core.Story.Models;
using SolTechnology.Core.Story.Orchestration;
using SolTechnology.Core.Story.Persistence;

namespace SolTechnology.Core.Story.Tests;

/// <summary>
/// Integration tests for pause/resume functionality with persistence.
/// Verifies end-to-end workflow with interactive chapters.
/// </summary>
[TestFixture]
public class PauseResumeIntegrationTests
{
    private IServiceProvider _serviceProvider = null!;
    private InMemoryStoryRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());

        _repository = new InMemoryStoryRepository();

        // Register repository
        services.AddSingleton<IStoryRepository>(_repository);

        // Register StoryManager
        services.AddScoped<StoryManager>();

        // Register test chapters
        services.AddTransient<OrderValidationChapter>();
        services.AddTransient<RequestCustomerDetailsChapter>();
        services.AddTransient<ProcessPaymentChapter>();
        services.AddTransient<SendConfirmationChapter>();

        // Register test story handler
        services.AddTransient<OrderProcessingStory>();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Test]
    public async Task Story_ShouldPause_AtInteractiveChapter()
    {
        // Arrange
        var manager = _serviceProvider.GetRequiredService<StoryManager>();
        var input = new OrderInput { OrderId = "ORD-001", Amount = 100.50m };

        // Act - Start the story
        var result = await manager.StartStory<OrderProcessingStory, OrderInput, OrderNarration, OrderOutput>(input);

        // Assert - Should pause at interactive chapter
        result.IsSuccess.Should().BeTrue();
        var storyInstance = result.Value!;

        storyInstance.Status.Should().Be(StoryStatus.WaitingForInput);
        storyInstance.CurrentChapter.Should().NotBeNull();
        storyInstance.CurrentChapter!.ChapterId.Should().Be("RequestCustomerDetailsChapter");
        storyInstance.CurrentChapter.RequiredData.Should().NotBeEmpty();
    }

    [Test]
    public async Task Story_ShouldResume_WithUserInput()
    {
        // Arrange
        var manager = _serviceProvider.GetRequiredService<StoryManager>();
        var input = new OrderInput { OrderId = "ORD-002", Amount = 200m };

        // Act - Start the story (should pause)
        var startResult = await manager.StartStory<OrderProcessingStory, OrderInput, OrderNarration, OrderOutput>(input);
        var storyId = startResult.Value!.StoryId;

        // Prepare user input
        var customerDetails = new CustomerDetails
        {
            Name = "John Doe",
            Email = "john@example.com",
            Address = "123 Main St"
        };
        var userInput = JsonSerializer.SerializeToElement(customerDetails);

        // Resume the story with user input
        var resumeResult = await manager.ResumeStory<OrderProcessingStory, OrderInput, OrderNarration, OrderOutput>(
            storyId,
            userInput);

        // Assert - Should complete successfully
        resumeResult.IsSuccess.Should().BeTrue();
        resumeResult.Value!.Status.Should().Be(StoryStatus.Completed);
    }

    [Test]
    public async Task Story_ShouldPreserveContext_AcrossPauseResume()
    {
        // Arrange
        var manager = _serviceProvider.GetRequiredService<StoryManager>();
        var input = new OrderInput { OrderId = "ORD-003", Amount = 150m };

        // Act - Start story
        var startResult = await manager.StartStory<OrderProcessingStory, OrderInput, OrderNarration, OrderOutput>(input);
        var storyId = startResult.Value!.StoryId;

        // Load story state
        var storyInstance = await _repository.FindById(storyId);
        var narration = JsonSerializer.Deserialize<OrderNarration>(storyInstance!.Context);

        // Assert - Context should be preserved
        narration.Should().NotBeNull();
        narration!.Input.OrderId.Should().Be("ORD-003");
        narration.Input.Amount.Should().Be(150m);
        narration.ValidationPassed.Should().BeTrue(); // From first chapter
    }

    [Test]
    public async Task Story_ShouldExecuteAllChapters_AfterResume()
    {
        // Arrange
        var manager = _serviceProvider.GetRequiredService<StoryManager>();
        var input = new OrderInput { OrderId = "ORD-004", Amount = 99.99m };

        // Act - Start and pause
        var startResult = await manager.StartStory<OrderProcessingStory, OrderInput, OrderNarration, OrderOutput>(input);
        var storyId = startResult.Value!.StoryId;

        // Resume with input
        var customerDetails = new CustomerDetails
        {
            Name = "Jane Smith",
            Email = "jane@example.com",
            Address = "456 Oak Ave"
        };
        var userInput = JsonSerializer.SerializeToElement(customerDetails);

        var resumeResult = await manager.ResumeStory<OrderProcessingStory, OrderInput, OrderNarration, OrderOutput>(
            storyId,
            userInput);

        // Assert - All chapters should have executed
        var finalState = await _repository.FindById(storyId);
        finalState!.History.Should().HaveCount(4); // All 4 chapters
        finalState.History.Select(h => h.ChapterId).Should().ContainInOrder(
            "OrderValidationChapter",
            "RequestCustomerDetailsChapter",
            "ProcessPaymentChapter",
            "SendConfirmationChapter"
        );
    }

    [Test]
    public async Task Story_ShouldHandleInvalidUserInput()
    {
        // Arrange
        var manager = _serviceProvider.GetRequiredService<StoryManager>();
        var input = new OrderInput { OrderId = "ORD-005", Amount = 50m };

        // Act - Start story
        var startResult = await manager.StartStory<OrderProcessingStory, OrderInput, OrderNarration, OrderOutput>(input);
        var storyId = startResult.Value!.StoryId;

        // Resume with invalid input (empty name)
        var invalidDetails = new CustomerDetails
        {
            Name = "",
            Email = "invalid@example.com",
            Address = "789 Pine Rd"
        };
        var userInput = JsonSerializer.SerializeToElement(invalidDetails);

        var resumeResult = await manager.ResumeStory<OrderProcessingStory, OrderInput, OrderNarration, OrderOutput>(
            storyId,
            userInput);

        // Assert - Should fail validation
        resumeResult.IsFailure.Should().BeTrue();
        resumeResult.Error!.Message.Should().Contain("name");
    }

    [Test]
    public async Task StoryManager_ShouldReturnError_WhenStoryNotFound()
    {
        // Arrange
        var manager = _serviceProvider.GetRequiredService<StoryManager>();

        // Act
        var result = await manager.GetStoryState("non-existent-id");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Message.Should().Contain("not found");
    }

    [Test]
    public async Task StoryManager_ShouldGetStoryState()
    {
        // Arrange
        var manager = _serviceProvider.GetRequiredService<StoryManager>();
        var input = new OrderInput { OrderId = "ORD-006", Amount = 75m };

        // Act - Start story
        var startResult = await manager.StartStory<OrderProcessingStory, OrderInput, OrderNarration, OrderOutput>(input);
        var storyId = startResult.Value!.StoryId;

        // Get story state
        var stateResult = await manager.GetStoryState(storyId);

        // Assert
        stateResult.IsSuccess.Should().BeTrue();
        stateResult.Value!.StoryId.Should().Be(storyId);
        stateResult.Value.Status.Should().Be(StoryStatus.WaitingForInput);
    }
}

#region Test Story and Chapters

public class OrderProcessingStory : StoryHandler<OrderInput, OrderNarration, OrderOutput>
{
    public OrderProcessingStory(
        IServiceProvider sp,
        ILogger<OrderProcessingStory> logger,
        StoryOptions? options = null)
        : base(sp, logger, options ?? StoryOptions.WithInMemoryPersistence())
    {
    }

    protected override async Task TellStory()
    {
        await Chapter<OrderValidationChapter>();
        await Chapter<RequestCustomerDetailsChapter>(); // Interactive - will pause here
        await Chapter<ProcessPaymentChapter>();
        await Chapter<SendConfirmationChapter>();
    }
}

public class OrderValidationChapter : Chapter<OrderNarration>
{
    public override Task<Result> Read(OrderNarration narration)
    {
        if (narration.Input.Amount <= 0)
        {
            return Result.FailAsTask("Order amount must be positive");
        }

        narration.ValidationPassed = true;
        return Result.SuccessAsTask();
    }
}

public class RequestCustomerDetailsChapter : InteractiveChapter<OrderNarration, CustomerDetails>
{
    public override Task<Result> ReadWithInput(OrderNarration narration, CustomerDetails userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput.Name))
        {
            return Result.FailAsTask("Customer name is required");
        }

        narration.CustomerName = userInput.Name;
        narration.CustomerEmail = userInput.Email;
        narration.CustomerAddress = userInput.Address;

        return Result.SuccessAsTask();
    }
}

public class ProcessPaymentChapter : Chapter<OrderNarration>
{
    public override Task<Result> Read(OrderNarration narration)
    {
        // Simulate payment processing
        narration.PaymentProcessed = true;
        narration.TransactionId = $"TXN-{Guid.NewGuid():N}";

        return Result.SuccessAsTask();
    }
}

public class SendConfirmationChapter : Chapter<OrderNarration>
{
    public override Task<Result> Read(OrderNarration narration)
    {
        // Populate output
        narration.Output.OrderId = narration.Input.OrderId;
        narration.Output.TransactionId = narration.TransactionId;
        narration.Output.CustomerName = narration.CustomerName;
        narration.Output.ConfirmationSent = true;

        return Result.SuccessAsTask();
    }
}

#endregion

#region Test Models

public class OrderInput
{
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class OrderOutput
{
    public string OrderId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public bool ConfirmationSent { get; set; }
}

public class OrderNarration : Narration<OrderInput, OrderOutput>
{
    public bool ValidationPassed { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;
    public bool PaymentProcessed { get; set; }
    public string TransactionId { get; set; } = string.Empty;
}

public class CustomerDetails
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

#endregion

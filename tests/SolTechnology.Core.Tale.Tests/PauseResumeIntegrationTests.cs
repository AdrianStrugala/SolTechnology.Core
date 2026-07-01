using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using SolTechnology.Core;
using SolTechnology.Core.Tale;
using SolTechnology.Core.Tale.Models;
using SolTechnology.Core.Tale.Orchestration;
using SolTechnology.Core.Tale.Persistence;
using SolTechnology.Core.Tale;

namespace SolTechnology.Core.Tale.Tests;

/// <summary>
/// Integration tests for pause/resume functionality with persistence.
/// Verifies end-to-end workflow with interactive chapters.
/// </summary>
[TestFixture]
public class PauseResumeIntegrationTests
{
    private IServiceProvider _serviceProvider = null!;
    private InMemoryTaleRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());

        _repository = new InMemoryTaleRepository();

        // Register TaleOptions with persistence
        services.AddSingleton(new TaleOptions());

        // Register repository
        services.AddSingleton<ITaleRepository>(_repository);

        // Register TaleManager
        services.AddScoped<TaleManager>();

        // Register test chapters
        services.AddTransient<OrderValidationChapter>();
        services.AddTransient<RequestCustomerDetailsChapter>();
        services.AddTransient<ProcessPaymentChapter>();
        services.AddTransient<SendConfirmationChapter>();

        // Register test story handler
        services.AddTransient<OrderProcessingTale>();

        _serviceProvider = services.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown()
    {
        if (_serviceProvider is IAsyncDisposable asyncDisposable)
        {
            asyncDisposable.DisposeAsync().GetAwaiter().GetResult();
        }
        else if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    [Test]
    public async Task Tale_ShouldPause_AtInteractiveChapter()
    {
        // Arrange
        var manager = _serviceProvider.GetRequiredService<TaleManager>();
        var input = new OrderInput { OrderId = "ORD-001", Amount = 100.50m };

        // Act - Start the story
        var result = await manager.StartStory<OrderProcessingTale, OrderInput, OrderContext, OrderOutput>(input);

        // Assert - Should pause at interactive chapter
        result.IsSuccess.Should().BeTrue();
        var storyInstance = result.Data!;

        storyInstance.Status.Should().Be(TaleStatus.WaitingForInput);
        storyInstance.CurrentChapter.Should().NotBeNull();
        storyInstance.CurrentChapter!.ChapterId.Should().Be("RequestCustomerDetailsChapter");
        storyInstance.CurrentChapter.RequiredData.Should().NotBeEmpty();
    }

    [Test]
    public async Task Tale_ShouldResume_WithUserInput()
    {
        // Arrange
        var manager = _serviceProvider.GetRequiredService<TaleManager>();
        var input = new OrderInput { OrderId = "ORD-002", Amount = 200m };

        // Act - Start the story (should pause)
        var startResult = await manager.StartStory<OrderProcessingTale, OrderInput, OrderContext, OrderOutput>(input);
        var storyId = startResult.Data!.TaleId;

        // Prepare user input
        var customerDetails = new CustomerDetails
        {
            Name = "John Doe",
            Email = "john@example.com",
            Address = "123 Main St"
        };
        var userInput = JsonSerializer.SerializeToElement(customerDetails);

        // Resume the story with user input
        var resumeResult = await manager.ResumeStory<OrderProcessingTale, OrderInput, OrderContext, OrderOutput>(
            storyId,
            userInput);

        // Assert - Should complete successfully
        resumeResult.IsSuccess.Should().BeTrue();
        resumeResult.Data!.Status.Should().Be(TaleStatus.Completed);
    }

    [Test]
    public async Task Tale_ShouldPreserveContext_AcrossPauseResume()
    {
        // Arrange
        var manager = _serviceProvider.GetRequiredService<TaleManager>();
        var input = new OrderInput { OrderId = "ORD-003", Amount = 150m };

        // Act - Start story
        var startResult = await manager.StartStory<OrderProcessingTale, OrderInput, OrderContext, OrderOutput>(input);
        var storyId = startResult.Data!.TaleId;

        // Load story state
        var storyInstance = await _repository.FindById(storyId);
        var context = JsonSerializer.Deserialize<OrderContext>(storyInstance!.Context);

        // Assert - Context should be preserved
        context.Should().NotBeNull();
        context!.Input.OrderId.Should().Be("ORD-003");
        context.Input.Amount.Should().Be(150m);
        context.ValidationPassed.Should().BeTrue(); // From first chapter
    }

    [Test]
    public async Task Tale_ShouldExecuteAllChapters_AfterResume()
    {
        // Arrange
        var manager = _serviceProvider.GetRequiredService<TaleManager>();
        var input = new OrderInput { OrderId = "ORD-004", Amount = 99.99m };

        // Act - Start and pause
        var startResult = await manager.StartStory<OrderProcessingTale, OrderInput, OrderContext, OrderOutput>(input);
        startResult.IsSuccess.Should().BeTrue();
        var storyId = startResult.Data!.TaleId;

        // Resume with input
        var customerDetails = new CustomerDetails
        {
            Name = "Jane Smith",
            Email = "jane@example.com",
            Address = "456 Oak Ave"
        };
        var userInput = JsonSerializer.SerializeToElement(customerDetails);

        var resumeResult = await manager.ResumeStory<OrderProcessingTale, OrderInput, OrderContext, OrderOutput>(
            storyId,
            userInput);

        // Assert
        resumeResult.IsSuccess.Should().BeTrue();

        // All chapters should have executed
        var finalState = await _repository.FindById(storyId);
        finalState.Should().NotBeNull();
        finalState!.History.Should().HaveCount(4); // All 4 chapters
        finalState.History.Select(h => h.ChapterId).Should().ContainInOrder(
            "OrderValidationChapter",
            "RequestCustomerDetailsChapter",
            "ProcessPaymentChapter",
            "SendConfirmationChapter"
        );
    }

    [Test]
    public async Task Tale_ShouldHandleInvalidUserInput()
    {
        // Arrange
        var manager = _serviceProvider.GetRequiredService<TaleManager>();
        var input = new OrderInput { OrderId = "ORD-005", Amount = 50m };

        // Act - Start story
        var startResult = await manager.StartStory<OrderProcessingTale, OrderInput, OrderContext, OrderOutput>(input);
        var storyId = startResult.Data!.TaleId;

        // Resume with invalid input (empty name)
        var invalidDetails = new CustomerDetails
        {
            Name = "",
            Email = "invalid@example.com",
            Address = "789 Pine Rd"
        };
        var userInput = JsonSerializer.SerializeToElement(invalidDetails);

        var resumeResult = await manager.ResumeStory<OrderProcessingTale, OrderInput, OrderContext, OrderOutput>(
            storyId,
            userInput);

        // Assert — validation failure keeps the story paused for retry
        resumeResult.IsSuccess.Should().BeTrue();
        resumeResult.Data!.Status.Should().Be(TaleStatus.WaitingForInput);
    }

    [Test]
    public async Task TaleManager_ShouldReturnError_WhenTaleNotFound()
    {
        // Arrange
        var manager = _serviceProvider.GetRequiredService<TaleManager>();

        // Act
        var result = await manager.GetStoryState(Auid.New("TST"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Message.Should().Contain("not found");
    }

    [Test]
    public async Task TaleManager_ShouldGetStoryState()
    {
        // Arrange
        var manager = _serviceProvider.GetRequiredService<TaleManager>();
        var input = new OrderInput { OrderId = "ORD-006", Amount = 75m };

        // Act - Start story
        var startResult = await manager.StartStory<OrderProcessingTale, OrderInput, OrderContext, OrderOutput>(input);
        var storyId = startResult.Data!.TaleId;

        // Get story state
        var stateResult = await manager.GetStoryState(storyId);

        // Assert
        stateResult.IsSuccess.Should().BeTrue();
        stateResult.Data!.TaleId.Should().Be(storyId);
        stateResult.Data.Status.Should().Be(TaleStatus.WaitingForInput);
    }
}

#region Test Tale and Chapters

public class OrderProcessingTale : TaleHandler<OrderInput, OrderContext, OrderOutput>
{
    public OrderProcessingTale(
        IServiceProvider sp,
        ILogger<OrderProcessingTale> logger)
        : base(sp, logger)
    {
    }

    protected override Tale<OrderOutput> Tell() =>
        Open<OrderValidationChapter>()
            .Read<RequestCustomerDetailsChapter>() // Interactive - will pause here
            .Read<ProcessPaymentChapter>()
            .Read<SendConfirmationChapter>()
            .Finale(ctx => ctx.Output);
}

public class OrderValidationChapter : Chapter<OrderContext>
{
    public override Task<Result> Read(OrderContext context)
    {
        if (context.Input.Amount <= 0)
        {
            return Result.FailAsTask("Order amount must be positive");
        }

        context.ValidationPassed = true;
        return Result.SuccessAsTask();
    }
}

public class RequestCustomerDetailsChapter : InteractiveChapter<OrderContext, CustomerDetails>
{
    public override Task<Result> ReadWithInput(OrderContext context, CustomerDetails userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput.Name))
        {
            return Result.FailAsTask("Customer name is required");
        }

        context.CustomerName = userInput.Name;
        context.CustomerEmail = userInput.Email;
        context.CustomerAddress = userInput.Address;

        return Result.SuccessAsTask();
    }
}

public class ProcessPaymentChapter : Chapter<OrderContext>
{
    public override Task<Result> Read(OrderContext context)
    {
        // Simulate payment processing
        context.PaymentProcessed = true;
        context.TransactionId = $"TXN-{Guid.NewGuid():N}";

        return Result.SuccessAsTask();
    }
}

public class SendConfirmationChapter : Chapter<OrderContext>
{
    public override Task<Result> Read(OrderContext context)
    {
        // Populate output
        context.Output.OrderId = context.Input.OrderId;
        context.Output.TransactionId = context.TransactionId;
        context.Output.CustomerName = context.CustomerName;
        context.Output.ConfirmationSent = true;

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

public class OrderContext : Context<OrderInput, OrderOutput>
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

using FluentAssertions;
using NUnit.Framework;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Story;
using SolTechnology.Core.Story.Models;

namespace SolTechnology.Core.Story.Tests;

/// <summary>
/// Tests for interactive chapters that require user input.
/// </summary>
[TestFixture]
public class InteractiveChapterTests
{
    [Test]
    public void InteractiveChapter_ShouldGenerateInputSchema_FromType()
    {
        // Arrange
        var chapter = new TestInteractiveChapter();

        // Act
        var schema = chapter.GetRequiredInputSchema();

        // Assert
        schema.Should().NotBeEmpty();
        schema.Should().Contain(f => f.Name == "CustomerName");
        schema.Should().Contain(f => f.Name == "Age");
        schema.Should().Contain(f => f.Name == "Email");
    }

    [Test]
    public void InteractiveChapter_Schema_ShouldIncludeCorrectTypes()
    {
        // Arrange
        var chapter = new TestInteractiveChapter();

        // Act
        var schema = chapter.GetRequiredInputSchema();

        // Assert
        var nameField = schema.First(f => f.Name == "CustomerName");
        nameField.Type.Should().Be("String");
        nameField.IsComplex.Should().BeFalse();

        var ageField = schema.First(f => f.Name == "Age");
        ageField.Type.Should().Be("Int32");
        ageField.IsComplex.Should().BeFalse();
    }

    [Test]
    public async Task InteractiveChapter_ShouldExecuteWithInput()
    {
        // Arrange
        var chapter = new TestInteractiveChapter();
        var context = new TestInteractiveContext
        {
            Input = new TestInteractiveInput()
        };
        var userInput = new CustomerDetailsInput
        {
            CustomerName = "John Doe",
            Age = 30,
            Email = "john@example.com"
        };

        // Act
        var result = await chapter.ReadWithInput(context, userInput);

        // Assert
        result.IsSuccess.Should().BeTrue();
        context.CustomerName.Should().Be("John Doe");
        context.CustomerAge.Should().Be(30);
    }

    [Test]
    public async Task InteractiveChapter_ShouldValidateUserInput()
    {
        // Arrange
        var chapter = new TestInteractiveChapter();
        var context = new TestInteractiveContext
        {
            Input = new TestInteractiveInput()
        };
        var invalidInput = new CustomerDetailsInput
        {
            CustomerName = "", // Empty name should fail
            Age = -1, // Negative age should fail
            Email = "invalid-email"
        };

        // Act
        var result = await chapter.ReadWithInput(context, invalidInput);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Message.Should().Contain("Customer name cannot be empty");
    }

    [Test]
    public void InteractiveChapter_ShouldThrow_WhenCalledWithoutInput()
    {
        // Arrange
        var chapter = new TestInteractiveChapter();
        var context = new TestInteractiveContext
        {
            Input = new TestInteractiveInput()
        };

        // Act & Assert
        var act = async () => await ((IChapter<TestInteractiveContext>)chapter).Read(context);

        act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*requires user input*");
    }

    [Test]
    public void InteractiveChapter_ShouldHaveChapterId()
    {
        // Arrange
        var chapter = new TestInteractiveChapter();

        // Act
        var chapterId = chapter.ChapterId;

        // Assert
        chapterId.Should().Be("TestInteractiveChapter");
    }

    [Test]
    public async Task InteractiveChapter_ShouldSupportComplexInputTypes()
    {
        // Arrange
        var chapter = new ComplexInputChapter();
        var context = new TestInteractiveContext
        {
            Input = new TestInteractiveInput()
        };
        var complexInput = new ComplexInput
        {
            OrderDetails = new OrderDetails
            {
                OrderId = "ORD-123",
                Items = new List<string> { "Item1", "Item2" }
            },
            Quantity = 5
        };

        // Act
        var result = await chapter.ReadWithInput(context, complexInput);

        // Assert
        result.IsSuccess.Should().BeTrue();
        context.ComplexData.Should().Be("Order: ORD-123, Quantity: 5");
    }

    [Test]
    public void InteractiveChapter_Schema_ShouldHandleComplexTypes()
    {
        // Arrange
        var chapter = new ComplexInputChapter();

        // Act
        var schema = chapter.GetRequiredInputSchema();

        // Assert
        var orderDetailsField = schema.First(f => f.Name == "OrderDetails");
        orderDetailsField.IsComplex.Should().BeTrue();
        orderDetailsField.Children.Should().NotBeEmpty();
        orderDetailsField.Children.Should().Contain(f => f.Name == "OrderId");
        orderDetailsField.Children.Should().Contain(f => f.Name == "Items");
    }
}

#region Test Interactive Chapters

public class TestInteractiveChapter : InteractiveChapter<TestInteractiveContext, CustomerDetailsInput>
{
    public override Task<Result> ReadWithInput(
        TestInteractiveContext context,
        CustomerDetailsInput userInput)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(userInput.CustomerName))
        {
            return Result.FailAsTask("Customer name cannot be empty");
        }

        if (userInput.Age < 0)
        {
            return Result.FailAsTask("Age cannot be negative");
        }

        // Process input
        context.CustomerName = userInput.CustomerName;
        context.CustomerAge = userInput.Age;
        context.CustomerEmail = userInput.Email;

        return Result.SuccessAsTask();
    }
}

public class ComplexInputChapter : InteractiveChapter<TestInteractiveContext, ComplexInput>
{
    public override Task<Result> ReadWithInput(
        TestInteractiveContext context,
        ComplexInput userInput)
    {
        context.ComplexData = $"Order: {userInput.OrderDetails.OrderId}, Quantity: {userInput.Quantity}";
        return Result.SuccessAsTask();
    }
}

#endregion

#region Test Input Models

public class CustomerDetailsInput
{
    public string CustomerName { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Email { get; set; } = string.Empty;
}

public class ComplexInput
{
    public OrderDetails OrderDetails { get; set; } = new();
    public int Quantity { get; set; }
}

public class OrderDetails
{
    public string OrderId { get; set; } = string.Empty;
    public List<string> Items { get; set; } = new();
}

#endregion

#region Test context

public class TestInteractiveInput
{
}

public class TestInteractiveOutput
{
    public string Result { get; set; } = string.Empty;
}

public class TestInteractiveContext : Context<TestInteractiveInput, TestInteractiveOutput>
{
    public string CustomerName { get; set; } = string.Empty;
    public int CustomerAge { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string ComplexData { get; set; } = string.Empty;
}

#endregion

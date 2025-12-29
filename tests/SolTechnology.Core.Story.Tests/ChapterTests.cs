using FluentAssertions;
using NUnit.Framework;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Story;

namespace SolTechnology.Core.Story.Tests;

/// <summary>
/// Tests for basic chapter execution.
/// Verifies that chapters can execute, modify context, return success/failure, and use DI.
/// </summary>
[TestFixture]
public class ChapterTests
{
    [Test]
    public async Task Chapter_ShouldExecuteSuccessfully()
    {
        // Arrange
        var chapter = new TestSuccessChapter();
        var context = new TestContext { Input = new TestInput { Value = 10 } };

        // Act
        var result = await chapter.Read(context);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Chapter_ShouldModifyContext()
    {
        // Arrange
        var chapter = new TestModifyContextChapter();
        var context = new TestContext { Input = new TestInput { Value = 10 } };

        // Act
        await chapter.Read(context);

        // Assert
        context.ProcessedValue.Should().Be(20); // Should double the input value
    }

    [Test]
    public async Task Chapter_ShouldReturnFailure_WhenBusinessLogicFails()
    {
        // Arrange
        var chapter = new TestFailureChapter();
        var context = new TestContext { Input = new TestInput { Value = -1 } };

        // Act
        var result = await chapter.Read(context);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Message.Should().Be("Negative values are not allowed");
    }

    [Test]
    public async Task Chapter_ShouldUseDependencyInjection()
    {
        // Arrange
        var testService = new TestService();
        var chapter = new TestChapterWithDI(testService);
        var context = new TestContext { Input = new TestInput { Value = 10 } };

        // Act
        await chapter.Read(context);

        // Assert
        context.ServiceResult.Should().Be("Service called with value: 10");
    }

    [Test]
    public void Chapter_ShouldHaveChapterId_BasedOnTypeName()
    {
        // Arrange
        var chapter = new TestSuccessChapter();

        // Act
        var chapterId = chapter.ChapterId;

        // Assert
        chapterId.Should().Be("TestSuccessChapter");
    }

    [Test]
    public void Chapter_ShouldAllowCustomChapterId()
    {
        // Arrange
        var chapter = new TestCustomIdChapter();

        // Act
        var chapterId = chapter.ChapterId;

        // Assert
        chapterId.Should().Be("CustomChapterId");
    }
}

#region Test Chapters

/// <summary>
/// Test chapter that always succeeds.
/// </summary>
public class TestSuccessChapter : Chapter<TestContext>
{
    public override Task<Result> Read(TestContext context)
    {
        return Result.SuccessAsTask();
    }
}

/// <summary>
/// Test chapter that modifies the context.
/// </summary>
public class TestModifyContextChapter : Chapter<TestContext>
{
    public override Task<Result> Read(TestContext context)
    {
        context.ProcessedValue = context.Input.Value * 2;
        return Result.SuccessAsTask();
    }
}

/// <summary>
/// Test chapter that fails on negative values.
/// </summary>
public class TestFailureChapter : Chapter<TestContext>
{
    public override Task<Result> Read(TestContext context)
    {
        if (context.Input.Value < 0)
        {
            return Result.FailAsTask("Negative values are not allowed");
        }

        return Result.SuccessAsTask();
    }
}

/// <summary>
/// Test chapter that uses dependency injection.
/// </summary>
public class TestChapterWithDI : Chapter<TestContext>
{
    private readonly TestService _service;

    public TestChapterWithDI(TestService service)
    {
        _service = service;
    }

    public override Task<Result> Read(TestContext context)
    {
        context.ServiceResult = _service.Process(context.Input.Value);
        return Result.SuccessAsTask();
    }
}

/// <summary>
/// Test chapter with a custom chapter ID.
/// </summary>
public class TestCustomIdChapter : Chapter<TestContext>
{
    public override string ChapterId => "CustomChapterId";

    public override Task<Result> Read(TestContext context)
    {
        return Result.SuccessAsTask();
    }
}

#endregion

#region Test Models

public class TestInput
{
    public int Value { get; set; }
}

public class TestOutput
{
    public int FinalValue { get; set; }
}

public class TestContext : Context<TestInput, TestOutput>
{
    public int ProcessedValue { get; set; }
    public string? ServiceResult { get; set; }
}

public class TestService
{
    public string Process(int value)
    {
        return $"Service called with value: {value}";
    }
}

#endregion

using FluentAssertions;
using NUnit.Framework;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Story;

namespace SolTechnology.Core.Story.Tests;

/// <summary>
/// Tests for basic chapter execution.
/// Verifies that chapters can execute, modify narration, return success/failure, and use DI.
/// </summary>
[TestFixture]
public class ChapterTests
{
    [Test]
    public async Task Chapter_ShouldExecuteSuccessfully()
    {
        // Arrange
        var chapter = new TestSuccessChapter();
        var narration = new TestNarration { Input = new TestInput { Value = 10 } };

        // Act
        var result = await chapter.Read(narration);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Chapter_ShouldModifyNarration()
    {
        // Arrange
        var chapter = new TestModifyNarrationChapter();
        var narration = new TestNarration { Input = new TestInput { Value = 10 } };

        // Act
        await chapter.Read(narration);

        // Assert
        narration.ProcessedValue.Should().Be(20); // Should double the input value
    }

    [Test]
    public async Task Chapter_ShouldReturnFailure_WhenBusinessLogicFails()
    {
        // Arrange
        var chapter = new TestFailureChapter();
        var narration = new TestNarration { Input = new TestInput { Value = -1 } };

        // Act
        var result = await chapter.Read(narration);

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
        var narration = new TestNarration { Input = new TestInput { Value = 10 } };

        // Act
        await chapter.Read(narration);

        // Assert
        narration.ServiceResult.Should().Be("Service called with value: 10");
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
public class TestSuccessChapter : Chapter<TestNarration>
{
    public override Task<Result> Read(TestNarration narration)
    {
        return Result.SuccessAsTask();
    }
}

/// <summary>
/// Test chapter that modifies the narration.
/// </summary>
public class TestModifyNarrationChapter : Chapter<TestNarration>
{
    public override Task<Result> Read(TestNarration narration)
    {
        narration.ProcessedValue = narration.Input.Value * 2;
        return Result.SuccessAsTask();
    }
}

/// <summary>
/// Test chapter that fails on negative values.
/// </summary>
public class TestFailureChapter : Chapter<TestNarration>
{
    public override Task<Result> Read(TestNarration narration)
    {
        if (narration.Input.Value < 0)
        {
            return Result.FailAsTask("Negative values are not allowed");
        }

        return Result.SuccessAsTask();
    }
}

/// <summary>
/// Test chapter that uses dependency injection.
/// </summary>
public class TestChapterWithDI : Chapter<TestNarration>
{
    private readonly TestService _service;

    public TestChapterWithDI(TestService service)
    {
        _service = service;
    }

    public override Task<Result> Read(TestNarration narration)
    {
        narration.ServiceResult = _service.Process(narration.Input.Value);
        return Result.SuccessAsTask();
    }
}

/// <summary>
/// Test chapter with a custom chapter ID.
/// </summary>
public class TestCustomIdChapter : Chapter<TestNarration>
{
    public override string ChapterId => "CustomChapterId";

    public override Task<Result> Read(TestNarration narration)
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

public class TestNarration : Narration<TestInput, TestOutput>
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

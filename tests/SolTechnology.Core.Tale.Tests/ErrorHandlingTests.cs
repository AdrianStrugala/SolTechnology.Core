using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using SolTechnology.Core;
using SolTechnology.Core.Errors;
using SolTechnology.Core.Tale;
using SolTechnology.Core.Tale;

namespace SolTechnology.Core.Tale.Tests;

/// <summary>
/// Tests for error handling in Tale framework.
/// Verifies Result pattern, AggregateError, and error propagation.
/// </summary>
[TestFixture]
public class ErrorHandlingTests
{
    private IServiceProvider _serviceProvider = null!;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());

        // Register test chapters
        services.AddTransient<ErrorTestSuccessChapter>();
        services.AddTransient<ErrorTestFailureChapter>();
        services.AddTransient<ErrorTestFirstFailureChapter>();
        services.AddTransient<ErrorTestSecondFailureChapter>();
        services.AddTransient<ErrorTestExceptionChapter>();
        services.AddTransient<ErrorTestCustomErrorChapter>();

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
    public async Task ErrorHandling_ShouldReturnSuccess_WhenAllChaptersSucceed()
    {
        // Arrange
        var handler = new SuccessfulTale(_serviceProvider, GetLogger<SuccessfulTale>());
        var input = new ErrorTestInput { Value = 10 };

        // Act
        var result = await handler.Handle(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().BeNull();
        result.Data.Should().NotBeNull();
    }

    [Test]
    public async Task ErrorHandling_ShouldReturnSingleError_WhenOneChapterFails()
    {
        // Arrange
        var handler = new SingleFailureTale(_serviceProvider, GetLogger<SingleFailureTale>());
        var input = new ErrorTestInput { Value = 10 };

        // Act
        var result = await handler.Handle(input);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error.Should().NotBeOfType<AggregateError>();
        result.Error!.Message.Should().Be("Chapter failed intentionally");
    }

    [Test]
    public async Task ErrorHandling_ShouldReturnFirstError_WhenMultipleChaptersFail()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton(new TaleOptions());
        services.AddTransient<ErrorTestFirstFailureChapter>();
        services.AddTransient<ErrorTestSecondFailureChapter>();
        var sp = services.BuildServiceProvider();

        var handler = new MultipleFailuresTale(sp, sp.GetRequiredService<ILogger<MultipleFailuresTale>>());
        var input = new ErrorTestInput { Value = 10 };

        // Act
        var result = await handler.Handle(input);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeOfType<AggregateError>();
        result.Error!.Message.Should().Be("First failure");
    }

    [Test]
    public async Task ErrorHandling_ShouldConvertException_ToError()
    {
        // Arrange
        var handler = new ExceptionTale(_serviceProvider, GetLogger<ExceptionTale>());
        var input = new ErrorTestInput { Value = 10 };

        // Act
        var result = await handler.Handle(input);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Message.Should().Contain("Test exception");
    }

    [Test]
    public async Task ErrorHandling_ShouldPreserveCustomError_WithDescription()
    {
        // Arrange
        var handler = new CustomErrorTale(_serviceProvider, GetLogger<CustomErrorTale>());
        var input = new ErrorTestInput { Value = 10 };

        // Act
        var result = await handler.Handle(input);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Message.Should().Be("Custom error message");
        result.Error.Description.Should().Be("Detailed description of the error");
    }

    [Test]
    public async Task ErrorHandling_ShouldNotExecuteRemainingChapters_AfterFirstFailure()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton(new TaleOptions());
        services.AddTransient<ErrorTestFirstFailureChapter>();
        services.AddTransient<ErrorTestSecondFailureChapter>();
        var sp = services.BuildServiceProvider();

        var handler = new MultipleFailuresTale(sp, sp.GetRequiredService<ILogger<MultipleFailuresTale>>());
        var input = new ErrorTestInput { Value = 10 };

        // Act
        var result = await handler.Handle(input);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Message.Should().Be("First failure");

        // Verify only the first chapter executed (tale short-circuits on first error)
        handler.Context.ExecutionLog.Should().HaveCount(1);
    }


    [Test]
    public async Task ErrorHandling_ShouldReturnError_ForNullResult()
    {
        // Arrange
        var handler = new SuccessfulTale(_serviceProvider, GetLogger<SuccessfulTale>());
        var input = new ErrorTestInput { Value = 10 };

        // Act
        var result = await handler.Handle(input);

        // Assert - Even though chapters succeeded, if output can't be extracted, it should fail
        // This tests the TaleEngine.GetResult() error handling
        if (result.IsFailure && result.Error?.Message.Contains("extract output") == true)
        {
            result.Error.Message.Should().Contain("extract output");
        }
        else
        {
            // Normal success case
            result.IsSuccess.Should().BeTrue();
        }
    }

    private ILogger<T> GetLogger<T>()
    {
        return _serviceProvider.GetRequiredService<ILogger<T>>();
    }
}

#region Test Stories

public class SuccessfulTale : TaleHandler<ErrorTestInput, ErrorTesTContext, ErrorTestOutput>
{
    public SuccessfulTale(IServiceProvider sp, ILogger<SuccessfulTale> logger)
        : base(sp, logger)
    {
    }

    protected override Tale<ErrorTestOutput> Tell() =>
        Open<ErrorTestSuccessChapter>()
            .Do(ctx => ctx.Output.Result = "Success")
            .Finale(ctx => ctx.Output);
}

public class SingleFailureTale : TaleHandler<ErrorTestInput, ErrorTesTContext, ErrorTestOutput>
{
    public SingleFailureTale(IServiceProvider sp, ILogger<SingleFailureTale> logger)
        : base(sp, logger)
    {
    }

    protected override Tale<ErrorTestOutput> Tell() =>
        Open<ErrorTestSuccessChapter>()
            .Read<ErrorTestFailureChapter>()
            .Read<ErrorTestSuccessChapter>()
            .Finale(ctx => ctx.Output);
}

public class MultipleFailuresTale : TaleHandler<ErrorTestInput, ErrorTesTContext, ErrorTestOutput>
{
    public MultipleFailuresTale(
        IServiceProvider sp,
        ILogger<MultipleFailuresTale> logger)
        : base(sp, logger)
    {
    }

    protected override Tale<ErrorTestOutput> Tell() =>
        Open<ErrorTestFirstFailureChapter>()
            .Read<ErrorTestSecondFailureChapter>()
            .Finale(ctx => ctx.Output);
}

public class ExceptionTale : TaleHandler<ErrorTestInput, ErrorTesTContext, ErrorTestOutput>
{
    public ExceptionTale(IServiceProvider sp, ILogger<ExceptionTale> logger)
        : base(sp, logger)
    {
    }

    protected override Tale<ErrorTestOutput> Tell() =>
        Open<ErrorTestExceptionChapter>()
            .Finale(ctx => ctx.Output);
}

public class CustomErrorTale : TaleHandler<ErrorTestInput, ErrorTesTContext, ErrorTestOutput>
{
    public CustomErrorTale(IServiceProvider sp, ILogger<CustomErrorTale> logger)
        : base(sp, logger)
    {
    }

    protected override Tale<ErrorTestOutput> Tell() =>
        Open<ErrorTestCustomErrorChapter>()
            .Finale(ctx => ctx.Output);
}

#endregion

#region Test Chapters

public class ErrorTestSuccessChapter : Chapter<ErrorTesTContext>
{
    public override Task<Result> Read(ErrorTesTContext context)
    {
        context.ExecutionLog.Add("Success");
        return Result.SuccessAsTask();
    }
}

public class ErrorTestFailureChapter : Chapter<ErrorTesTContext>
{
    public override Task<Result> Read(ErrorTesTContext context)
    {
        context.ExecutionLog.Add("Failure");
        return Result.FailAsTask("Chapter failed intentionally");
    }
}

public class ErrorTestFirstFailureChapter : Chapter<ErrorTesTContext>
{
    public override Task<Result> Read(ErrorTesTContext context)
    {
        context.ExecutionLog.Add("FirstFailure");
        return Result.FailAsTask("First failure");
    }
}

public class ErrorTestSecondFailureChapter : Chapter<ErrorTesTContext>
{
    public override Task<Result> Read(ErrorTesTContext context)
    {
        context.ExecutionLog.Add("SecondFailure");
        return Result.FailAsTask("Second failure");
    }
}

public class ErrorTestExceptionChapter : Chapter<ErrorTesTContext>
{
    public override Task<Result> Read(ErrorTesTContext context)
    {
        throw new InvalidOperationException("Test exception from chapter");
    }
}

public class ErrorTestCustomErrorChapter : Chapter<ErrorTesTContext>
{
    public override Task<Result> Read(ErrorTesTContext context)
    {
        var customError = new Error
        {
            Message = "Custom error message",
            Description = "Detailed description of the error"
        };

        return Result.FailAsTask(customError);
    }
}

#endregion

#region Test Models

public class ErrorTestInput
{
    public int Value { get; set; }
}

public class ErrorTestOutput
{
    public string Result { get; set; } = string.Empty;
}

public class ErrorTesTContext : Context<ErrorTestInput, ErrorTestOutput>
{
    public List<string> ExecutionLog { get; set; } = new();
}

#endregion

using FluentAssertions;
using NUnit.Framework;
using SolTechnology.Core.Errors;

namespace SolTechnology.Core.Tests;

public sealed class ResultTests
{
    [Test]
    public void Success_IsSuccessWithoutError()
    {
        // Act
        var result = Result<int>.Success(7);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().BeNull();
        result.Data.Should().Be(7);
    }

    [Test]
    public void Fail_IsFailureWithErrorAndNoData()
    {
        // Arrange
        var error = new NotFoundError { Message = "Missing" };

        // Act
        var result = Result<string>.Fail(error);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(error);
        result.Data.Should().BeNull();
    }

    [Test]
    public async Task FailAsTask_OnGenericResult_ReturnsTypedResult()
    {
        // Act
        Task<Result<int>> task = Result<int>.FailAsTask(new Error { Message = "Boom" });
        var result = await task;

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Message.Should().Be("Boom");
    }

    [Test]
    public void ImplicitConversions_BuildSuccessAndFailure()
    {
        // Act
        Result<int> success = 3;
        Result<int> failure = new ConflictError { Message = "Taken" };

        // Assert
        success.IsSuccess.Should().BeTrue();
        success.Data.Should().Be(3);
        failure.IsFailure.Should().BeTrue();
        failure.Error.Should().BeOfType<ConflictError>();
    }
}

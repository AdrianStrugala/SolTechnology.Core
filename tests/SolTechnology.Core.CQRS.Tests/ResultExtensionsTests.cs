using FluentAssertions;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.CQRS.Errors;

namespace SolTechnology.Core.CQRS.Tests;

[TestFixture]
public class ResultExtensionsTests
{
    [Test]
    public void Map_OnSuccess_TransformsData()
    {
        // Arrange
        var result = Result<int>.Success(5);

        // Act
        var mapped = result.Map(x => x * 2);

        // Assert
        mapped.IsSuccess.Should().BeTrue();
        mapped.Data.Should().Be(10);
    }

    [Test]
    public void Map_OnFailure_ShortCircuits()
    {
        // Arrange
        var result = Result<int>.Fail("bad");

        // Act
        var mapped = result.Map(x => x * 2);

        // Assert
        mapped.IsFailure.Should().BeTrue();
        mapped.Error!.Message.Should().Be("bad");
    }

    [Test]
    public async Task Bind_OnSuccess_ChainsOperation()
    {
        // Arrange
        var result = Result<int>.Success(3);

        // Act
        var bound = await result.Bind(x => Task.FromResult(Result<string>.Success($"val:{x}")));

        // Assert
        bound.IsSuccess.Should().BeTrue();
        bound.Data.Should().Be("val:3");
    }

    [Test]
    public async Task Bind_OnFailure_ShortCircuits()
    {
        // Arrange
        var result = Result<int>.Fail("nope");

        // Act
        var bound = await result.Bind(x => Task.FromResult(Result<string>.Success($"val:{x}")));

        // Assert
        bound.IsFailure.Should().BeTrue();
    }

    [Test]
    public void Tap_OnSuccess_ExecutesSideEffect()
    {
        // Arrange
        var result = Result<int>.Success(7);
        var sideEffect = 0;

        // Act
        result.Tap(x => sideEffect = x);

        // Assert
        sideEffect.Should().Be(7);
    }

    [Test]
    public void Tap_OnFailure_DoesNotExecute()
    {
        // Arrange
        var result = Result<int>.Fail("err");
        var sideEffect = 0;

        // Act
        result.Tap(x => sideEffect = x);

        // Assert
        sideEffect.Should().Be(0);
    }

    [Test]
    public void Match_OnSuccess_CallsSuccessBranch()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var output = result.Match(x => $"ok:{x}", e => $"fail:{e.Message}");

        // Assert
        output.Should().Be("ok:42");
    }

    [Test]
    public void Match_OnFailure_CallsFailureBranch()
    {
        // Arrange
        var result = Result<int>.Fail("oops");

        // Act
        var output = result.Match(x => $"ok:{x}", e => $"fail:{e.Message}");

        // Assert
        output.Should().Be("fail:oops");
    }

    [Test]
    public void Ensure_WhenPredicatePasses_ReturnsOriginal()
    {
        // Arrange
        var result = Result<int>.Success(10);

        // Act
        var ensured = result.Ensure(x => x > 5, new Error { Message = "too small" });

        // Assert
        ensured.IsSuccess.Should().BeTrue();
        ensured.Data.Should().Be(10);
    }

    [Test]
    public void Ensure_WhenPredicateFails_ReturnsFail()
    {
        // Arrange
        var result = Result<int>.Success(3);

        // Act
        var ensured = result.Ensure(x => x > 5, new Error { Message = "too small" });

        // Assert
        ensured.IsFailure.Should().BeTrue();
        ensured.Error!.Message.Should().Be("too small");
    }

    [Test]
    public void Ensure_OnFailure_ShortCircuits()
    {
        // Arrange
        var result = Result<int>.Fail("already bad");

        // Act
        var ensured = result.Ensure(x => x > 5, new Error { Message = "too small" });

        // Assert
        ensured.IsFailure.Should().BeTrue();
        ensured.Error!.Message.Should().Be("already bad");
    }
}


using FluentAssertions;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Errors;

namespace SolTechnology.Core.CQRS.Tests.PipelineBehaviors;

[TestFixture]
public class LoggingPipelineBehaviorChronicleOutcomeTests : LoggingPipelineBehaviorChronicleTestBase
{
    [Test]
    public async Task Handle_FailedResult_StepFailedWithTypeAndMessageOnly()
    {
        // Arrange
        var error = new ValidationError
        {
            Message = "Validation failed",
            Errors = new Dictionary<string, string[]> { ["Email"] = ["jan.kowalski@example.com is not allowed"] }
        };

        // Act
        await Sut<ProbeCommand, Result>().Handle(new ProbeCommand("Warsaw"), () => Task.FromResult(Result.Fail(error)), CancellationToken.None);

        // Assert
        var report = Host.Reports.Should().ContainSingle().Which;
        report.Level.Should().Be(LogLevel.Error);
        report.Message.Should().StartWith("Chronicle [ProbeCommand] [Failed]")
            .And.Contain("Error=[ValidationError: Validation failed]").And.NotContain("jan.kowalski");
    }

    [Test]
    public async Task Handle_Throws_StepFailedAndRethrows()
    {
        // Act
        var act = () => Sut<ProbeCommand, Result>().Handle(new ProbeCommand("Warsaw"),
            () => throw new InvalidOperationException("boom"), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        var report = Host.Reports.Should().ContainSingle().Which;
        report.Exception.Should().BeOfType<InvalidOperationException>();
        report.Message.Should().Contain("[Failed]").And.Contain("Error=[InvalidOperationException: boom]");
    }

    [Test]
    public async Task Handle_CallerCancelled_StepCancelled()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act
        var act = () => Sut<ProbeCommand, Result>().Handle(new ProbeCommand("Warsaw"),
            () => throw new OperationCanceledException(cancellation.Token), cancellation.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        var report = Host.Reports.Should().ContainSingle().Which;
        report.Level.Should().Be(LogLevel.Warning);
        report.Message.Should().StartWith("Chronicle [ProbeCommand] [Cancelled]").And.NotContain("Error=");
    }
}

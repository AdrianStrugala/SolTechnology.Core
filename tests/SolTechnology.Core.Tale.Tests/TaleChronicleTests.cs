using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using SolTechnology.Core.Logging.Chronicles;

namespace SolTechnology.Core.Tale.Tests;

[TestFixture]
public class TaleChronicleTests : TaleChronicleTestBase
{
    [Test]
    public async Task Handle_ThreeChapters_TaleStepHasThreeChapterChildrenInOrder()
    {
        // Act
        var result = await Handle<ChronicleThreeChapterTale>();

        // Assert
        result.IsSuccess.Should().BeTrue();
        var report = Reports.Should().ContainSingle().Which;
        report.Level.Should().Be(LogLevel.Information);
        report.Message.Should().StartWith("Chronicle [Tale ChronicleThreeChapterTale] [Ok]");
        var lines = TreeLines();
        lines[0].Should().StartWith("Tale ChronicleThreeChapterTale [@0 ms]").And.Contain(" TaleId=");
        lines[1..].Select(l => l[..l.IndexOf(" [@", StringComparison.Ordinal)]).Should().Equal(
            "|- Chapter ChronicleFirstChapter", "|- Chapter ChronicleSecondChapter", "`- Chapter ChronicleThirdChapter");
    }

    [TestCase(1, "[failed] Error=[NotFoundError: City [Atlantis] not found]")]
    [TestCase(0, "[failed] Error=[InvalidOperationException: boom]")]
    public async Task Handle_ChapterFails_ChapterAndTaleStepsFailedAndLaterChaptersAbsent(int value, string chapterOutcome)
    {
        // Act
        var result = await Handle<ChronicleFailingTale>(value);

        // Assert
        result.IsFailure.Should().BeTrue();
        Reports.Should().ContainSingle().Which.Message.Should().StartWith("Chronicle [Tale ChronicleFailingTale] [Failed]")
            .And.NotContain("ChronicleThirdChapter");
        TreeLines()[0].Should().Contain($"[failed] Error=[{result.Error!.GetType().Name}: {result.Error.Message}]");
        TreeLines().Should().ContainSingle(l => l.Contains("Chapter ChronicleFailingChapter")).Which.Should().EndWith(chapterOutcome);
    }

    [Test]
    public async Task Handle_CallerCancelled_ChapterCancelled()
    {
        // Act
        var result = await Handle<ChronicleCancelledTale>();

        // Assert
        result.Error.Should().BeOfType<TaleCancelledError>();
        var report = Reports.Should().ContainSingle().Which;
        report.Level.Should().Be(LogLevel.Warning);
        report.Message.Should().StartWith("Chronicle [Tale ChronicleCancelledTale] [Cancelled]").And.NotContain("ChronicleThirdChapter");
        TreeLines().Should().ContainSingle(l => l.Contains("Chapter ChronicleCancellingChapter")).Which.Should().EndWith("[cancelled]");
    }

    [Test]
    public async Task Handle_InteractiveChapterPauses_ChapterOkWithWaitingForInput()
    {
        // Act
        var result = await Handle<LifecycleTaleV1>();

        // Assert
        result.Error.Should().BeOfType<TalePausedError>();
        var lines = TreeLines();
        Reports.Single().Message.Should().StartWith("Chronicle [Tale LifecycleTaleV1] [Ok]").And.NotContain("LifecycleCompleteChapter");
        lines[0].Should().EndWith("Outcome=WaitingForInput");
        lines.Should().ContainSingle(l => l.Contains("Chapter LifecyclePauseChapter")).Which.Should().EndWith("[ok] Outcome=WaitingForInput");
    }

    [Test]
    public async Task Handle_InsideRoot_TaleIsChildOfAmbientStep()
    {
        // Act
        using (Chronicle.Begin("ImportCities"))
        {
            await Handle<ChronicleThreeChapterTale>();
        }

        // Assert
        Reports.Should().ContainSingle().Which.Message.Should().StartWith("Chronicle [ImportCities] [Ok]")
            .And.Contain("\n`- Tale ChronicleThreeChapterTale [@").And.Contain("\n   |- Chapter ChronicleFirstChapter [@");
    }

    [Test]
    public async Task Handle_ChronicleNotRegistered_RunsUnchanged()
    {
        // Arrange
        Services.Dispose();
        Services = Build(services => services.RemoveAll<IChronicle>());

        // Act
        var result = await Handle<ChronicleThreeChapterTale>();

        // Assert
        result.IsSuccess.Should().BeTrue();
        Services.GetService<IChronicle>().Should().BeNull();
        Reports.Should().BeEmpty();
    }

    [Test]
    public void AddSolTale_RegistersChronicle()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();

        // Act
        services.AddSolTale(assemblies: typeof(TaleChronicleTests).Assembly);

        // Assert
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        provider.GetService<IChronicle>().Should().BeOfType(Chronicle.GetType());
    }
}
